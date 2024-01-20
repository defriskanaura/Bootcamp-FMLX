//user bisa liat private dan public, uncomment aja yang di comment
import { IReservation } from "../models/responses/IReservation";
import { Api } from "../api/Api.ts";
import { HttpStatusCode } from "axios";
import { DateTime } from "luxon";
import { batch, computed, effect, signal } from "@preact/signals-react";
import { profile, getUserId, isAdmin, isEngineer, isManager } from "./ProfileManager.ts";

export const reservations = signal<Array<IReservation>>([])
export const reservationsPerUser = signal<Array<IReservation>>([])
export const availableReservations = signal<Array<IReservation>>([])
export const reservationCountPerPage = signal<number>(20)
export const totalReservationCount = signal<number>(0)
export const isLoadingReservations = signal<boolean>(false)
export const isLoadingAddReservation = signal<boolean>(false)
export const isLoadingUpdateReservation = signal<boolean>(false)
export const isLoadingRemoveReservation = signal<boolean>(false)
export const isLoadingFindReservation = signal<boolean>(false)
export const lastReservationErrorMessage = signal<string | null>(null)
export const findReservationErrorMessage = signal<string | null>(null)
export const reservationPage = signal<number>(1)
export const reservationModal = signal<boolean>(false)
export const reservationModalDelete = signal<boolean>(false)
export const reservationModalUpdate = signal<boolean>(false)
export const statusAdd = signal<boolean>(false)
export const statusUpdate = signal<boolean>(false)
export const statusDelete = signal<boolean>(false)

export const reservationStartIndex = computed(() => {
    const pageIndex = reservationPage.value < 1 ? 0 : reservationPage.value - 1
    return pageIndex * reservationCountPerPage.value
})
export const reservationPageCount = computed(() => {
    const count = totalReservationCount.value / reservationCountPerPage.value
    return Math.ceil(count)
})

effect(() => {
    if ((isAdmin.value) && reservationCountPerPage.value > 0 && reservationPage.value > 0) {
        refreshReservations().catch()
    }
    if ((isEngineer.value || isManager.value) && reservationCountPerPage.value > 0 && reservationPage.value > 0) {
        refreshReservationsPerUser().catch()
    }
})

export async function refreshReservationsTotalCount() {
    const countResponse = await Api.get(`reservation/count`)

    if (countResponse.status !== HttpStatusCode.Ok) {
        return
    }

    const count: { totalCount: number } = countResponse.data;

    if (!count) {
        return
    }

    totalReservationCount.value = count.totalCount ?? 0
}

export async function refreshReservations() {
    if (isLoadingReservations.peek()) {
        return
    }

    isLoadingReservations.value = true
    await refreshReservationsTotalCount()

    const params = new URLSearchParams()
    params.set("startIndex", (Math.max(reservationPage.value - 1, 0) * reservationCountPerPage.value).toFixed())
    params.set("count", reservationCountPerPage.value.toFixed())

    const response = await Api.get(`reservation?${params.toString()}`)

    if (response.status !== HttpStatusCode.Ok) {
        batch(() => {
            reservations.value = []
            isLoadingReservations.value = false
        })
        return
    }

    batch(() => {
        reservations.value = response.data
        isLoadingReservations.value = false
    })
}

export async function refreshReservationsPerUser() {
    if (isLoadingReservations.peek()) {
        return
    }

    isLoadingReservations.value = true
    await refreshReservationsTotalCount()
    /*
    const paramsUser = new URLSearchParams()
    paramsUser.set("startIndex", (Math.max(reservationPage.value - 1, 0) * reservationCountPerPage.value).toFixed())
    paramsUser.set("count", reservationCountPerPage.value.toFixed())
    paramsUser.set("visibility", "private")
    // @ts-ignore
    paramsUser.set("userId", getUserId.value.toString())

    const paramsPublic = new URLSearchParams()
    paramsPublic.set("startIndex", (Math.max(reservationPage.value - 1, 0) * reservationCountPerPage.value).toFixed())
    paramsPublic.set("count", reservationCountPerPage.value.toFixed())
    paramsPublic.set("visibility", "public")

    const responseUser = await Api.get(`reservation?${paramsUser.toString()}`)
    const responsePublic = await Api.get(`reservation?${paramsPublic.toString()}`)
    */

    const paramsUser = new URLSearchParams()
    paramsUser.set("startIndex", (Math.max(reservationPage.value - 1, 0) * reservationCountPerPage.value).toFixed())
    paramsUser.set("count", reservationCountPerPage.value.toFixed())
    // @ts-ignore
    paramsUser.set("userId", getUserId.value.toString())

    const responseUser = await Api.get(`reservation?${paramsUser.toString()}`)

    if (responseUser.status !== HttpStatusCode.Ok) {
        batch(() => {
            reservations.value = []
            isLoadingReservations.value = false
        })
        return
    }

    batch(() => {
        /*
        reservations.value = responsePublic.data.concat(responseUser.data)
        */
        reservations.value = responseUser.data
        isLoadingReservations.value = false
    })
}

export async function addReservation(reservation: IReservationRequest) {
    if (isLoadingAddReservation.peek()) {
        return
    }

    isLoadingAddReservation.value = true
    const response = await Api.put("reservation", reservation)

    if (response.status !== HttpStatusCode.Ok && response.status !== HttpStatusCode.Created) {
        batch(() => {
            lastReservationErrorMessage.value = response.data
            isLoadingAddReservation.value = false
            reservationModal.value = true
            statusAdd.value = false
        })
        return
    }
    if (isManager.value == true) {
        await refreshReservationsPerUser()
    }
    else if (isEngineer.value == true) {
        await refreshReservationsPerUser()
    }
    else if (isAdmin.value == true) {
        await refreshReservations()
    }
    isLoadingAddReservation.value = false
    reservationModal.value = true
    statusAdd.value = true
}

export async function updateReservation(reservation: IReservation) {

    if (isLoadingUpdateReservation.peek()) {
        return
    }

    isLoadingUpdateReservation.value = true
    const response = await Api.patchReservation(`reservation/${getUserId.value}`, reservation)

    if (response.status !== HttpStatusCode.Ok) {
        batch(() => {
            lastReservationErrorMessage.value = response.data
            isLoadingUpdateReservation.value = false
            reservationModalUpdate.value = true
            statusUpdate.value = false
        })
        return
    }
    if (isManager.value == true) {
        await refreshReservationsPerUser()
    }
    else if (isEngineer.value == true) {
        await refreshReservationsPerUser()
    }
    else if (isAdmin.value == true) {
        await refreshReservations()
    }
    isLoadingUpdateReservation.value = false
    reservationModalUpdate.value = true
    statusUpdate.value = true
}

export async function removeReservation(reservation: IReservation) {
    if (isLoadingRemoveReservation.peek()) {
        return
    }

    isLoadingRemoveReservation.value = true
    const response = await Api.deleteReservation('reservation', `${getUserId.value}/${reservation.reservationId}`);

    if (response.status !== HttpStatusCode.Ok) {
        batch(() => {
            lastReservationErrorMessage.value = response.data
            isLoadingRemoveReservation.value = false
            reservationModalDelete.value = true
            statusDelete.value = false
        })
        return
    }
    if (isManager.value == true) {
        await refreshReservationsPerUser()
    }
    else if (isEngineer.value == true) {
        await refreshReservationsPerUser()
    }
    else if (isAdmin.value == true) {
        await refreshReservations()
    }
    isLoadingRemoveReservation.value = false
    reservationModalDelete.value = true
    statusDelete.value = true
}

export async function findReservation(start: DateTime, end: DateTime, capacity: number) {
    if (isLoadingFindReservation.peek()) {
        return
    }

    const startDate = start.toISO()
    const endDate = end.toISO()

    if (!startDate || !endDate || capacity < 0) {
        return
    }

    const params = new URLSearchParams()
    params.set("startDate", startDate)
    params.set("endDate", endDate)
    params.set("capacity", capacity.toFixed())

    batch(() => {
        findReservationErrorMessage.value = null
        isLoadingFindReservation.value = true
    })

    const response = await Api.get(`reservation/find?${params.toString()}`)

    if (response.status !== HttpStatusCode.Ok) {
        batch(() => {
            findReservationErrorMessage.value = response.data
            availableReservations.value = []
            isLoadingFindReservation.value = false
        })
        return
    }

    batch(() => {
        availableReservations.value = response.data
        isLoadingFindReservation.value = false
    })
}
