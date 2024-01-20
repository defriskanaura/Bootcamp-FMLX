using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Google.Apis.Chat.v1;
using Google.Apis.Chat.v1.Data;
using System;
using System.IO;
using System.Threading.Tasks;

class Program
{
	static async Task Main(string[] args)
	{
		string serviceAccountKeyFilePath = "credentials.json";
		string calendarId = "f13e67e08bc985d2184f42b4851e9e9dbb9c85b59aa6b04317d0e131740982bc@group.calendar.google.com"; // Ganti dengan Calendar ID yang sesuai
		string[] Scopes = {ChatService.Scope.ChatBot};
		
		GoogleCredential credential1;
		GoogleCredential credential2;
		iMethod method;
		using (var stream = new FileStream(serviceAccountKeyFilePath, FileMode.Open, FileAccess.Read))
		{
			credential1 = GoogleCredential.FromStream(stream)
				.CreateScoped(CalendarService.Scope.Calendar);
			credential2 = GoogleCredential.FromStream(stream)
				.CreateScoped(Scopes);
		}

		var service = new CalendarService(new BaseClientService.Initializer
		{
			HttpClientInitializer = credential1,
			ApplicationName = "Google Calendar API Service Account",
		});
		var serviceGoogleChat = new ChatService(new BaseClientService.Initializer()
		{
			HttpClientInitializer = credential2,
			ApplicationName = "Google Chat API Service Account",
		});
		//var delete = service.Events.Delete("af89b3fbd65dd8e361897d0de22a88f5b0a4d08db3a393b2d24b05b797af99d2@group.calendar.google.com", "vgtsse6rqcj4h12a8hhrc0ipn0").Execute();
		// // Buat request untuk mengambil event
		// var request = service.Events.List(calendarId);
		// request.TimeMin = DateTime.Now;
		// request.ShowDeleted = false;
		// request.SingleEvents = true;
		// request.MaxResults = 10;
		// request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

		// // Eksekusi request dan cetak hasil
		// var events = await request.ExecuteAsync();
		// Console.WriteLine("Upcoming events:");
		// if (events.Items != null && events.Items.Count > 0)
		// {
		// 	foreach (var eventItem in events.Items)
		// 	{
		// 		string when = eventItem.Start.DateTime.ToString();
		// 		if (String.IsNullOrEmpty(when))
		// 		{
		// 			when = eventItem.Start.Date;
		// 		}
		// 		Console.WriteLine($"{eventItem.Summary} ({when})");
		// 	}
		// }
		// else
		// {
		// 	Console.WriteLine("No upcoming events found.");
		// }
		
		// // Create a new event
		// var newEvent = new Event()
		// {
		// 	Summary = "Google Calendar API Event",
		// 	Location = "800 Howard St., San Francisco, CA 94103",
		// 	Description = "A chance to hear more about Google's developer products.",
		// 	Start = new EventDateTime()
		// 	{
		// 		DateTime = new DateTime(2024, 1, 20, 10, 0, 0),
		// 		TimeZone = "America/Los_Angeles",
		// 	},
		// 	End = new EventDateTime()
		// 	{
		// 		DateTime = new DateTime(2024, 1, 20, 11, 0, 0),
		// 		TimeZone = "America/Los_Angeles",
		// 	},
		// };

		// var request1 = service.Events.Insert(newEvent, calendarId);
		// var createdEvent = await request1.ExecuteAsync();
		// Console.WriteLine("Event created: {0}", createdEvent.HtmlLink);
		// Add an ACL rule
		// var rule = new AclRule
		// {
		// 	Role = "writer",
		// 	Scope = new AclRule.ScopeData
		// 	{
		// 		Type = "user",
		// 		Value = "sangsterodult@gmail.com" //Added email
		// 	}
		// };
		// Console.WriteLine("tipe data rule: {0}", rule.GetType());
		// method = new Method();
		// var aclRequest = method.InsertAcl(service, rule, calendarId);
		// //var aclRequest = service.Acl.Insert(rule, calendarId);
		// Console.WriteLine("tipe data aclRequest: {0}", aclRequest.GetType());
		// Console.WriteLine("ACL rule added: {0}", aclRequest.Id);
		
		// var newCalendar = new Calendar
		// {
		// 	Summary = "Bubun",
		// 	TimeZone = "Asia/Jakarta"  // Set zona waktu ke Jakarta
		// };

		// var createdCalendar = await service.Calendars.Insert(newCalendar).ExecuteAsync();
		// Console.WriteLine("Calendar created: {0}", createdCalendar.Id);
		
		var kalendarId = "602bbea6f54c8f533a8c98ee6b7552db9d0ea505d6511eb01a4e15e22e39fb46@group.calendar.google.com";
		Calendar kalendar = service.Calendars.Get(kalendarId).Execute();
		Events request = service.Events.List(kalendarId).Execute();
		string syncToken = request.NextSyncToken;
		Console.WriteLine("sync token pertama: {0}", syncToken);
		
		var newEvent1 = new Event()
		{
			Summary = "Ngapain yaaa",
			Location = "Manayah",
			Description = "A chance to hear more about Google's developer products.",
			Start = new EventDateTime()
			{
				DateTime = new DateTime(2024, 1, 19, 10, 0, 0),
				TimeZone = "Asia/Jakarta",
			},
			End = new EventDateTime()
			{
				DateTime = new DateTime(2024, 1, 19, 11, 0, 0),
				TimeZone = "Asia/Jakarta",
			},
		};
		
		var request2 = service.Events.Insert(newEvent1, kalendarId);
		var createdEvent1 = await request2.ExecuteAsync();
		
		Events request1 = service.Events.List(kalendarId).Execute();
		string syncToken1 = request1.NextSyncToken;
		Console.WriteLine("sync token pertama: {0}", syncToken1);
		
		// var request = service.Events.List(kalendarId);
		// request.ShowDeleted = false;
		// request.SingleEvents = true;
		// Events events;
		// events = await request.ExecuteAsync();
		// syncToken = events.NextSyncToken;
		// Console.WriteLine(syncToken);
		// Event cuy = service.Events.Get(kalendarId, "gm7ehlp5j9kot50vts48qnv0as").Execute();
		// Console.WriteLine(cuy.Summary);
		// Console.WriteLine(request.GetType());
		// var request2 = await service.Acl.List(kalendarId).ExecuteAsync();
		// Console.WriteLine(request2.GetType());
		// var rule = new AclRule
		// {
		// 	Role = "owner",
		// 	Scope = new AclRule.ScopeData
		// 	{
		// 		Type = "user",
		// 		Value = "meeting-room@meetingroomproject-410303.iam.gserviceaccount.com" //Added email
		// 	}
		// };
		// Console.WriteLine("tipe data rule: {0}", rule.GetType());
		// method = new Method();
		// var aclRequest = method.InsertAcl(service, rule, kalendarId);
		// //var aclRequest = service.Acl.Insert(rule, calendarId);
		// Console.WriteLine("ACL rule added: {0}", aclRequest.Id);
		
		// var rule2 = new AclRule
		// {
		// 	Role = "owner",
		// 	Scope = new AclRule.ScopeData
		// 	{
		// 		Type = "user",
		// 		Value = "meetingroom.batch7@gmail.com" //Added email
		// 	}
		// };
		// Console.WriteLine("tipe data rule: {0}", rule.GetType());
		// method = new Method();
		// var aclRequest2 = method.InsertAcl(service, rule2, kalendarId);
		// //var aclRequest = service.Acl.Insert(rule, calendarId);
		// Console.WriteLine("ACL rule added: {0}", aclRequest2.Id);
		
		// var newEvent1 = new Event()
		// {
		// 	Summary = "Ngapain yaaa",
		// 	Location = "Manayah",
		// 	Description = "A chance to hear more about Google's developer products.",
		// 	Start = new EventDateTime()
		// 	{
		// 		DateTime = new DateTime(2024, 1, 12, 10, 0, 0),
		// 		TimeZone = "Asia/Jakarta",
		// 	},
		// 	End = new EventDateTime()
		// 	{
		// 		DateTime = new DateTime(2024, 1, 12, 11, 0, 0),
		// 		TimeZone = "Asia/Jakarta",
		// 	},
		// };
		
		// var request2 = service.Events.Insert(newEvent1, kalendarId);
		// var createdEvent1 = await request2.ExecuteAsync();
		// Console.WriteLine("Event created: {0}", createdEvent1.HtmlLink);
		// Console.WriteLine("Event id: {0}", createdEvent1.Id);
		
		//var eventId = "7d5h0vim7hdkvarg790k9oddu0";
		
		// var request3 = service.Events.Delete(kalendarId, createdEvent1.Id);
		// var deleteEvent = await request3.ExecuteAsync();
		// Console.WriteLine("tipe data request3: {0}", request3.GetType());
		// Console.WriteLine("tipe data deleteEvent: {0}", deleteEvent.GetType());
		

		//var createdRule = await aclRequest.ExecuteAsync();
		// Console.WriteLine("tipe data createdRule: {0}", createdRule.GetType());
		// Console.WriteLine("ACL rule added: {0}", createdRule.Id);
	}
}

interface iMethod
{
	public Task<AclRule> InsertAcl(CalendarService service, AclRule body, string calendarId);
}
class Method : iMethod 
{
	public async Task<AclRule> InsertAcl(CalendarService service, AclRule body, string calendarId) 
	{
		return await service.Acl.Insert(body, calendarId).ExecuteAsync();
	}
	
}
//  using Google.Apis.Calendar.v3;
//  using Google.Apis.Services;
//  class Program
// 	{
// 		const string ApiKey = "AIzaSyBQB3NBosXLnDk3UBjB0L90lZRMC_x4vwY";
// 		const string CalendarId = "id.indonesian#holiday@group.v.calendar.google.com";

// 		static async Task Main(string[] args)
// 		{
// 			Console.WriteLine("Hello World!");

// 			var service = new CalendarService(new BaseClientService.Initializer()
// 			{
// 				ApiKey = ApiKey,
// 				ApplicationName = "Api key example"
// 			});

// 			var request = service.Events.List(CalendarId);
// 			request.Fields = "items(summary,start,end)";
// 			var response = await request.ExecuteAsync();

// 			foreach (var item in response.Items)
// 			{
// 				Console.WriteLine($"Holiday: {item.Summary} start: {item.Start} end: {item.End}");
// 			}

// 			Console.ReadLine();
// 		}
// 	}

		// var events = _calendarService.GetCalendarEventList(room.CalendarId).Execute();

		// if (room.SyncToken != events.NextSyncToken)
		// {
		// 	_logger.LogWarning("is it right {0}", room.SyncToken);
		// 	Event selectedEvent = _calendarService.GetEvent(room.CalendarId, reservation.EventId);
		// 	reservation.Description = selectedEvent.Summary;
		// 	reservation.Description = selectedEvent.Description;
		// 	reservation.End = (DateTime)selectedEvent.End.DateTime;
		// 	reservation.Start = (DateTime)selectedEvent.Start.DateTime;
		// 	var newToken = events.NextSyncToken.ToString();
		// 	var rooms = await _roomRepository.GetAsync(room.RoomId);
		// 	room.SyncToken = newToken;
		// 	_logger.LogWarning("is it wrong {0}", room.SyncToken);
		// 	try {
		// 		rooms.ApplySync(room);
		// 		_roomRepository.Update(rooms);
		// 		_logger.LogWarning("is it ejhfewj {0}", rooms.SyncToken);
		// 	}
		// 	catch(Exception e)
		// 	{
		// 		_logger.LogWarning(e.Message);
		// 	}
		// 	_reservationRepository.Update(reservation);
		// 	return BadRequest("Reservation is already updated via Google Calendar, please refresh this page and add the changes again");
		// }
		
		// var requestedEvent = await _calendarService.GetEventAsync(room.CalendarId, reservation.EventId);
		// requestedEvent.Summary = reservation.Description;
		// requestedEvent.Description = reservation.Description;
		// requestedEvent.End = new EventDateTime { DateTime = reservation.End,  TimeZone = "Asia/Jakarta"};
		// requestedEvent.Start = new EventDateTime { DateTime = reservation.Start, TimeZone = "Asia/Jakarta"};
		// var result = await _calendarService.UpdateEventAsync(requestedEvent, room.CalendarId, reservation.EventId);
		
		
		// if (room.SyncToken != events.NextSyncToken) {
		// 	reservation.Description = requestedEvent.Summary;
		// 	reservation.Description = requestedEvent.Description;
		// 	reservation.End = (DateTime)requestedEvent.End.DateTime;
		// 	reservation.Start = (DateTime)requestedEvent.Start.DateTime;
			
		// 	var newToken = events.NextSyncToken.ToString();
		// 	_logger.LogWarning("newtoken {0}", newToken);
		// 	_logger.LogWarning("room token room lama {0}", room.SyncToken);
			
		// 	var rooms = await _roomRepository.GetAsync(room.RoomId);
		// 	_logger.LogWarning("room token roomS lama {0}", room.SyncToken);
			
		// 	room.SyncToken = newToken;
			
		// 	rooms.ApplySync(room);
		// 	_logger.LogWarning("room token room baru cek di database {0}", rooms.SyncToken);
			
		// 	var commitStatus = await TryCommitRoom();

		// 	if (commitStatus is not null)
		// 	{
		// 		return commitStatus;
		// 	}
		// }
		
				// foreach (var ev in events.Execute().Items)
		// {
		// 	if (ev.Id == reservation.EventId)
		// 	{
		// 		if (ev.Status == "cancelled")
		// 		{
		// 			_logger.LogWarning("gak aktif");
		// 			//code if the event is not active anymore
		// 		}
		// 	}
		// }
		

		
		// if (room.SyncToken != events.NextSyncToken) {
		// 	reservation.Description = requestedEvent.Summary;
		// 	reservation.Description = requestedEvent.Description;
		// 	reservation.End = (DateTime)requestedEvent.End.DateTime;
		// 	reservation.Start = (DateTime)requestedEvent.Start.DateTime;
			
		// 	var newToken = events.NextSyncToken.ToString();
		// 	_logger.LogWarning("newtoken {0}", newToken);
		// 	_logger.LogWarning("room token room lama {0}", room.SyncToken);
			
		// 	var rooms = await _roomRepository.GetAsync(room.RoomId);
		// 	_logger.LogWarning("room token roomS lama {0}", room.SyncToken);
			
		// 	room.SyncToken = newToken;
			
		// 	rooms.ApplySync(room);
		// 	_logger.LogWarning("room token room baru cek di database {0}", rooms.SyncToken);
			
		// 	var commitStatus = await TryCommitRoom();

		// 	if (commitStatus is not null)
		// 	{
		// 		return commitStatus;
		// 	}
		// }
		// var room = _roomRepository.Query().FirstOrDefault(r => r.RoomId == reservation.RoomId);
		// var events = _calendarService.GetCalendarEventList(room.CalendarId);
		// var newToken = events.Execute().NextSyncToken.ToString();
		// room.SyncToken = newToken;
		// room.SyncToken = newToken;
		// 		var commitStatusRoom = await TryCommitRoom();
		// 		if (commitStatusRoom is not null)
		// 		{
		// 			return commitStatusRoom;
		// 		}
		// //deleting event in database because it is already deleted in calendar using sync token
		// events.SyncToken = room.SyncToken;
		// events.SingleEvents = true;
		// var targetEvent = events.Execute().Items.FirstOrDefault(ev => ev.Id == reservation.EventId);
		// if (targetEvent != null)
		// {
		// 	if (targetEvent.Status == "cancelled")
		// 	{
		// 		_logger.LogWarning("Event is not active anymore");
		// 		_reservationRepository.Remove(reservation);
		// 		var commitStatusReservation = await TryCommitReservation();
		// 		if (commitStatusReservation is not null)
		// 		{
		// 			return commitStatusReservation;
		// 		}
		// 		//next step : room sync token must be updated, create a new command
		// 		var newToken = events.Execute().NextSyncToken.ToString();
		// 		room.SyncToken = newToken;
		// 		var commitStatusRoom = await TryCommitRoom();
		// 		if (commitStatusRoom is not null)
		// 		{
		// 			return commitStatusRoom;
		// 		}
		// 		return BadRequest("The reservation you want to be edited is no longer available in Calendar, please refresh the page!");
		// 	}
		// }