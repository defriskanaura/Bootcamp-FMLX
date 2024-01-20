using System.Globalization;
using System.Linq.Expressions;
using System.Security.Claims;
using Google.Apis.Calendar.v3.Data;
using MeetingRoomBooking.Database.Entities;
using Microsoft.AspNetCore.Mvc;
using MeetingRoomBooking.Api.Extensions;
using MeetingRoomBooking.Api.GoogleServices.Interfaces;
using MeetingRoomBooking.Api.Models.Requests;
using MeetingRoomBooking.Api.Models.Responses;
using MeetingRoomBooking.Database.Entities.Enums;
using MeetingRoomBooking.Database.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using MeetingRoomBooking.Database.Entities.Collections;
using Event = Google.Apis.Calendar.v3.Data.Event;
using Email;

namespace MeetingRoomBooking.Api.Controllers;

[Route("api/[controller]")]
public class ReservationController : ControllerBase
{
	private readonly ILogger<ReservationController> _logger;
	private readonly IRepository<Reservation> _reservationRepository;
	private readonly IRepository<User> _userRepository;
	private readonly IRepository<Room> _roomRepository;
	private readonly IRepository<Activity> _activityRepository;
	private readonly IGoogleCalendarService _calendarService;
	private readonly IEmailService _emailService;
	

	public ReservationController(
		ILogger<ReservationController> logger,
		IRepository<Reservation> reservationRepository,
		IRepository<User> userRepository,
		IRepository<Room> roomRepository,
		IRepository<Activity> activityRepository,
		IGoogleCalendarService calendarService,
		IEmailService emailService
	)
	{
		_logger = logger;
		_reservationRepository = reservationRepository;
		_userRepository = userRepository;
		_roomRepository = roomRepository;
		_activityRepository = activityRepository;
		_calendarService = calendarService;
		_emailService = emailService;
	}

	[Authorize]
	[HttpGet("count")]
	public ActionResult<CountResponse> GetReservationsCount(
		[FromQuery] Guid? userId = null,
		[FromQuery] Guid? roomId = null,
		[FromQuery] string? startDate = null,
		[FromQuery] string? endDate = null,
		[FromQuery] string? visibility = null,
		[FromQuery] bool? recurring = null,
		[FromQuery] bool? active = null
	)
	{
		return Ok(
			new CountResponse
			{
				Count = _reservationRepository.Query().Count(r =>
					(userId == null || r.UserId == userId) &&
					(roomId == null || r.RoomId == roomId) &&
					(startDate == null || r.Start >= DateTime.Parse(startDate)) &&
					(endDate == null || r.End <= DateTime.Parse(endDate)) &&
					(visibility == null || r.Visibility == visibility) &&
					(recurring == null || (string)r.Recurrence == "[]" == recurring) &&
					(active == null || (active == true ? r.End >= DateTime.Today : r.End < DateTime.Today))
				),
				TotalCount = _reservationRepository.Query().Count()
			}
		);
	}

	[Authorize]
	[HttpGet]
	public ActionResult<IEnumerable<ReservationResponse>> GetReservations(
		[FromQuery] string? orderBy = null,
		[FromQuery] bool? descending = null,
		[FromQuery] Guid? userId = null,
		[FromQuery] Guid? roomId = null,
		[FromQuery] string? startDate = null,
		[FromQuery] string? endDate = null,
		[FromQuery] string? visibility = null,
		[FromQuery] bool? recurring = null,
		[FromQuery] bool? active = null,
		[FromQuery] int? startIndex = null,
		[FromQuery] int? count = null
	)
	{
		if (startIndex is not { } itemStartIndex)
		{
			itemStartIndex = 0;
		}

		if (count is not { } itemCount)
		{
			itemCount = _reservationRepository.Query().Count();
		}

		if (itemStartIndex < 0 || itemCount < 0)
		{
			_logger.LogError("startIndex and itemCount cannot be negative.");
			return BadRequest("startIndex and itemCount cannot be negative.");
		}

		if (startDate is not null && !DateTime.TryParse(startDate, CultureInfo.InvariantCulture, out _))
		{
			_logger.LogError("startDate has invalid format.");
			return BadRequest("startDate has invalid format.");
		}

		if (endDate is not null && !DateTime.TryParse(endDate, CultureInfo.InvariantCulture, out _))
		{
			_logger.LogError("endDate has invalid format.");
			return BadRequest("endDate has invalid format.");
		}

		if (descending is not { } reverse)
		{
			reverse = orderBy == null;
		}

		if (orderBy is not { } sortBy)
		{
			sortBy = "date-created";
		}

		Expression<Func<Reservation, object>> expression = sortBy.ToLowerInvariant() switch
		{
			"start-date" => r => r.Start,
			"end-date" => r => r.End,
			"visibility" => r => r.Visibility,
			"date-modified" => r => r.DateModified,
			_ => r => r.DateCreated
		};

		var reservations = _reservationRepository.Query().Include(r => r.User).ThenInclude(r => r.Role)
			.Include(r => r.Room)
			.Where(r =>
				(userId == null || r.UserId == userId) &&
				(roomId == null || r.RoomId == roomId) &&
				(startDate == null || r.Start >= DateTime.Parse(startDate)) &&
				(endDate == null || r.End <= DateTime.Parse(endDate)) &&
				(visibility == null || r.Visibility == visibility) &&
				(recurring == null || (string)r.Recurrence == "[]" == recurring) &&
				(active == null || (active == true ? r.End >= DateTime.Today : r.End < DateTime.Today))
			)
			.OrderBy(reverse, expression)
			.Skip(itemStartIndex)
			.Take(itemCount)
			.ToList()
			.ConvertAll(ReservationResponse.FromReservationNew);
		
		// TODO: If the user is not an admin or not the owner of the reservation, we should strip [User] from the [ReservationResponse] instance in case the reservation visibility is private
		return Ok(reservations);
	}

	[HttpGet("{id:guid}")]
	public async Task<ActionResult<ReservationResponse>> GetReservation([FromRoute] Guid? id)
	{
		if (id is not { } reservationId)
		{
			_logger.LogWarning("Invalid reservation id");
			return BadRequest("Invalid reservation id.");
		}

		var reservation = await _reservationRepository.GetAsync(reservationId);

		if (reservation is not null)
		{
			_logger.LogInformation("Get reservation successfuly");
			return Ok(ReservationResponse.FromReservation(reservation));
		}

		_logger.LogError("Reservation with id {id} was not found.", id);
		return NotFound($"Reservation with id {id} was not found.");
	}

	[HttpPost]
	public async Task<ActionResult<ReservationResponse>> PostReservation([FromBody] ReservationRequest? request)
	{
		var reservation = request?.ToReservation();

		if (reservation is null)
		{
			_logger.LogWarning("One parameter missing");
			return BadRequest("One or more required parameters are missing in the request.");
		}

		var room = await _roomRepository.GetAsync(reservation.RoomId);
		if (room is null)
		{
			_logger.LogWarning("Room not found");
			return NotFound("Room Not Found");
		}
		string calendarId = room.CalendarId;

		var recurrence = reservation.Recurrence;
		string recurrenceResult = String.Empty;
		foreach (var i in recurrence)
		{
			string temp = i.Replace("-", "");
			if (recurrenceResult == String.Empty)
			{
				recurrenceResult += temp;
				continue;
			}
			recurrenceResult += ";" + temp;
		}

	 
		Event body = new Event()
		{
			Summary = reservation.Description,
			Description = reservation.Description,
			End = new EventDateTime { DateTime = reservation.End,  TimeZone = "Asia/Jakarta"},
			Start = new EventDateTime { DateTime = reservation.Start, TimeZone = "Asia/Jakarta"}
		};
	   
		if (reservation.Recurrence.Count > 1)
		{
			body.Recurrence = new String[]
			{
				recurrenceResult
			};
		}

		var result = await _calendarService.InsertEventAsync(body, calendarId);
		reservation.EventId = result.Id;
		

		await _reservationRepository.AddAsync(reservation);
		
		if (await _reservationRepository.TryCommit() is { } e)
		{
			_logger.LogError("An error has occurred: {error}", e.Message);
			return StatusCode(500, "There is something wrong while processing your request.");
		}
		
		reservation = await _reservationRepository.GetAsync(reservation.ReservationId);
		
		if (reservation is null)
		{
			_logger.LogError("Cannot get reservation back after being added. ReservationId: {id}",
				reservation?.ReservationId);
			return StatusCode(500, "There is something wrong while processing your request.");
		}
		
		var user = _userRepository.Query().FirstOrDefault(r => r.UserId == reservation.UserId);
		var rooms = _roomRepository.Query().FirstOrDefault(r => r.RoomId == reservation.RoomId);
		if (user.Username != "administrator")
		{
			_emailService.SendCreatedReservationEmail(user, rooms, reservation);
		}
		
		await AddActivity(ActivityAction.Add, reservation);
		_logger.LogInformation("New event with ID {id} has been created." , reservation.ReservationId);
		return Ok(ReservationResponse.FromReservation(reservation));
	}

	[HttpPut]
	public async Task<ActionResult<ReservationResponse>> PutEvent([FromBody] ReservationRequest? request)
	{
	   
		return await PostReservation(request);
	}

	[HttpPatch]
	public async Task<ActionResult<ReservationResponse>> PatchReservation([FromBody] ReservationRequest? request)
	{
		if (request?.ReservationId is not { } reservationId)
		{
			_logger.LogWarning("PatchReservation called with missing ReservationId");
			return BadRequest("One or more required parameters are missing in the request.");
		}
		
		 _logger.LogInformation("Attempting to patch reservation with ID {ReservationId}", reservationId);
		var reservation = await _reservationRepository.GetAsync(reservationId);
		var room = _roomRepository.Query().FirstOrDefault(r => r.RoomId == reservation.RoomId);

		if (reservation is null)
		{
			_logger.LogWarning("Unable to find reservation with ID {ReservationId} for patching", reservationId);
			return NotFound($"Unable to update user. User with id {reservationId} was not found.");
		}

		reservation.Apply(request);
		_reservationRepository.Update(reservation);

		if (await _reservationRepository.TryCommit() is { } e)
		{
			_logger.LogError("An error has occurred: {error}", e.Message);
			return StatusCode(500, "There is something wrong while processing your request.");
		}

		reservation = await _reservationRepository.GetAsync(reservation.ReservationId);

		if (reservation is null)
		{
			_logger.LogError("Cannot get reservation back after being added. ReservationId: {id}",
				reservation?.ReservationId);
			return StatusCode(500, "There is something wrong while processing your request.");
		}
		
		var user = _userRepository.Query().FirstOrDefault(r => r.UserId == reservation.UserId);
		if (user.Username != "administrator")
		{
			_emailService.SendUpdatedReservationEmail(user, room, reservation);
		}
		await AddActivity(ActivityAction.Update, reservation);
		_logger.LogInformation("Details for event with ID {id} has been changed." , reservation.ReservationId);
		return Ok(ReservationResponse.FromReservation(reservation));
	}
	[HttpPatch("{userid:guid}")]
	public async Task<ActionResult<ReservationResponse>> PatchReservationByID([FromRoute]Guid? userid ,[FromBody] ReservationRequest? request)
	{
		if (request?.ReservationId is not { } reservationId)
		{
			_logger.LogWarning("PatchReservation called with missing ReservationId");
			return BadRequest("One or more required parameters are missing in the request.");
		}
		
		 _logger.LogInformation("Attempting to patch reservation with ID {ReservationId}", reservationId);
		var reservation = await _reservationRepository.GetAsync(reservationId);

		if (reservation is null)
		{
			_logger.LogWarning("Unable to find reservation with ID {ReservationId} for patching", reservationId);
			return NotFound($"Unable to update user. User with id {reservationId} was not found.");
		}
		
		var userEditor = _userRepository.Query().FirstOrDefault(r => r.UserId == userid);
		var userOwner = _userRepository.Query().FirstOrDefault(r => r.UserId == reservation.UserId);
		var room = _roomRepository.Query().FirstOrDefault(r => r.RoomId == reservation.RoomId);
		var rooms = await _roomRepository.GetAsync(room.RoomId); //dfjdl
		var events = _calendarService.GetCalendarEventList(room.CalendarId); //fdfd

		//deleting event in database because it is already deleted in calendar using sync token
		events.SyncToken = room.SyncToken;
		events.SingleEvents = true;
		var targetEvent = events.Execute().Items.FirstOrDefault(ev => ev.Id == reservation.EventId);
		if (targetEvent != null)
		{
			if (targetEvent.Status == "cancelled")
			{
				_logger.LogWarning("Event is not active anymore");
				_reservationRepository.Remove(reservation);
				var commitStatusReservation = await TryCommitReservation();
				if (commitStatusReservation is not null)
				{
					return commitStatusReservation;
				}
				//next step : room sync token must be updated, create a new command
				var newToken = events.Execute().NextSyncToken.ToString();
				room.SyncToken = newToken;
				var commitStatusRoom = await TryCommitRoom();
				if (commitStatusRoom is not null)
				{
					return commitStatusRoom;
				}
				return BadRequest("The reservation you want to be edited is no longer available in Calendar, please refresh the page!");
			}
		}
		
		if (userEditor == null)
		{
			_logger.LogError("Invalid user id.");
			return BadRequest("Invalid user id.");
		}
		if (userEditor.Username == "administrator")
		{	
			reservation.Apply(request);
			_reservationRepository.Update(reservation);
			if (userEditor.UserId != userOwner.UserId)
			{
				_emailService.SendUpdatedByAdminEmail(userOwner, room, reservation);
			}
		}
		if (userEditor.Username != "administrator")
		{	
			if (userEditor.UserId == userOwner.UserId)
			{
				reservation.Apply(request);
				_reservationRepository.Update(reservation);
				_emailService.SendUpdatedReservationEmail(userOwner, room, reservation);
			}
			if (userEditor.UserId != userOwner.UserId)
			{
				_logger.LogError("Non-admin user cannot edit other user's booking!");
				return BadRequest("Non-admin user cannot edit other user's booking!");
			}
		}

		reservation = await _reservationRepository.GetAsync(reservation.ReservationId);

		if (reservation is null)
		{
			_logger.LogError("Cannot get reservation back after being added. ReservationId: {id}",
				reservation?.ReservationId);
			return StatusCode(500, "There is something wrong while processing your request.");
		}
		
		var requestedEvent = await _calendarService.GetEventAsync(room.CalendarId, reservation.EventId);
		requestedEvent.Summary = reservation.Description;
		requestedEvent.Description = reservation.Description;
		requestedEvent.End = new EventDateTime { DateTime = reservation.End,  TimeZone = "Asia/Jakarta"};
		requestedEvent.Start = new EventDateTime { DateTime = reservation.Start, TimeZone = "Asia/Jakarta"};
		var result = await _calendarService.UpdateEventAsync(requestedEvent, room.CalendarId, reservation.EventId);
		
		await AddActivity(ActivityAction.Update, reservation);
		_logger.LogInformation("Details for event with ID {id} has been changed." , reservation.ReservationId);
		return Ok(ReservationResponse.FromReservation(reservation));
	}
	
	[HttpDelete("{id:guid}")]
	public async Task<ActionResult<ReservationResponse>> DeleteReservation([FromRoute] Guid? id)
	{
		if (id is not { } reservationId)
		{
			_logger.LogError("Invalid reservation id");
			return BadRequest("Invalid reservation id.");
		}

		var reservation = await _reservationRepository.GetAsync(reservationId);
		if (reservation is null)
		{
			_logger.LogError("Reservation with id {id} was not found.", id);
			return NotFound($"Reservation with id {id} was not found.");
		}
		
		var room = await _roomRepository.GetAsync(reservation.RoomId);
		if (room == null)
		{
			_logger.LogError("Room is not there");
			return BadRequest("Room is not there.");
		}

		_reservationRepository.Remove(reservation);
		var delEvent = await _calendarService.DeleteEventAsync(room.CalendarId, reservation.EventId);
	
		await AddActivity(ActivityAction.Delete, reservation);
		_logger.LogInformation("Event with ID {id} has been deleted." , reservation.ReservationId);
		return Ok(ReservationResponse.FromReservation(reservation));
	}
	
	[HttpDelete("{userid:guid}/{reservationid:guid}")]
	public async Task<ActionResult<ReservationResponse>> DeleteReservationKnowingComitter([FromRoute] Guid? userid, [FromRoute] Guid? reservationid)
	{
		if (reservationid is not { } reservationId)
		{
			_logger.LogError("Invalid reservation id");
			return BadRequest("Invalid reservation id.");
		}
		
		var reservation = await _reservationRepository.GetAsync(reservationId);
		if (reservation is null)
		{
			_logger.LogError("Reservation with id {id} was not found.", reservationid);
			return NotFound($"Reservation with id {reservationid} was not found.");
		}
		
		var room = await _roomRepository.GetAsync(reservation.RoomId);
		if (room == null)
		{
			_logger.LogError("Room is not there");
			return BadRequest("Room is not there.");
		}
		
		var userDeleter = _userRepository.Query().FirstOrDefault(r => r.UserId == userid);
		var userOwner = _userRepository.Query().FirstOrDefault(r => r.UserId == reservation.UserId);
		if (userDeleter == null)
		{
			_logger.LogError("Invalid user id.");
			return BadRequest("Invalid user id.");
		}
		if (userDeleter.Username == "administrator")
		{	
			_reservationRepository.Remove(reservation);
			var delEvent = await _calendarService.DeleteEventAsync(room.CalendarId, reservation.EventId);
			if (userDeleter.UserId != userOwner.UserId)
			{
				_emailService.SendDeletedByAdminEmail(userOwner, room, reservation);
			}
		}
		if (userDeleter.Username != "administrator")
		{	
			if (userDeleter.UserId == userOwner.UserId)
			{
				_reservationRepository.Remove(reservation);
				var delEvent = await _calendarService.DeleteEventAsync(room.CalendarId, reservation.EventId);
				_emailService.SendDeletedEventEmail(userOwner, room, reservation);
			}
			if (userDeleter.UserId != userOwner.UserId)
			{
				_logger.LogError("Non-admin user cannot delete other user's booking!");
				return BadRequest("Non-admin user cannot delete other user's booking!");
			}
		}
		if (await _reservationRepository.TryCommit() is { } e)
		{
			_logger.LogError("An error has occurred: {error}", e.Message);
			return StatusCode(500, "There is something wrong while processing your request.");
		}
		await AddActivity(ActivityAction.Delete, reservation);
		_logger.LogInformation("Event with ID {id} has been deleted." , reservation.ReservationId);
		return Ok(ReservationResponse.FromReservation(reservation));
	}
	
	private async Task<BadRequestObjectResult?> TryCommitRoom()
	{
		try
		{
			await _roomRepository.CommitAsync();
			return null;
		}
		catch (Exception e)
		{
			_logger.LogError("An error has occured: {error}", e.Message);
			return BadRequest(e.Message);
		}
	}
	
	private async Task<BadRequestObjectResult?> TryCommitReservation()
	{
		try
		{
			await _reservationRepository.CommitAsync();
			return null;
		}
		catch (Exception e)
		{
			_logger.LogError("An error has occured: {error}", e.Message);
			return BadRequest(e.Message);
		}
	}


	private async Task AddActivity(ActivityAction action, Reservation reservation)
	{
		var userId = (HttpContext.User.Identity as ClaimsIdentity)?.FindFirst("user_id")?.Value;

		if (userId == null || !Guid.TryParse(userId, out var id))
		{
			return;
		}

		var author = await _userRepository.GetAsync(id);

		if (author == null)
		{
			return;
		}

		await _activityRepository.AddReservationActivity(action, author, reservation);
		await _activityRepository.TryCommit();
	}
}