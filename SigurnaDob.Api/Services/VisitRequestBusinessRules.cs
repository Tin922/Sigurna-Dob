using SigurnaDob.Shared.Constants;
using SigurnaDob.Shared.Models;

namespace SigurnaDob.Api.Services;

public static class VisitRequestBusinessRules
{
    public static bool IsVisitInPast(DateTime requestedVisitAtUtc, DateTime utcNow) =>
        requestedVisitAtUtc.Date < utcNow.Date;

    public static string? ValidateNewRequest(DateTime requestedVisitAtUtc, DateTime utcNow)
    {
        if (IsVisitInPast(requestedVisitAtUtc, utcNow))
            return "Termin posjeta mora biti u budućnosti.";

        return null;
    }

    public static string? ValidateApprove(VisitRequest request, DateTime utcNow)
    {
        if (request.VisitRequestStatusId != VisitRequestStatusIds.Received)
            return "Odobriti se može samo zahtjev u statusu Zaprimljeno.";

        if (IsVisitInPast(request.RequestedVisitAt, utcNow))
            return "Posjet se ne može odobriti za datum u prošlosti.";

        return null;
    }

    public static string? ValidateReject(VisitRequest request)
    {
        if (request.VisitRequestStatusId != VisitRequestStatusIds.Received)
            return "Odbiti se može samo zahtjev u statusu Zaprimljeno.";

        return null;
    }

    public static string? ValidateMarkHeld(VisitRequest request)
    {
        if (request.VisitRequestStatusId != VisitRequestStatusIds.Approved)
            return "Posjet se može označiti održanim samo iz statusa Odobreno.";

        return null;
    }

    public static string? ValidateCancel(VisitRequest request, bool isFamilyMember)
    {
        if (isFamilyMember)
        {
            if (request.VisitRequestStatusId is not (
                VisitRequestStatusIds.Received or VisitRequestStatusIds.Approved))
                return "Obitelj može otkazati samo zaprimljen ili odobren zahtjev.";
        }
        else if (request.VisitRequestStatusId is VisitRequestStatusIds.Held or VisitRequestStatusIds.Cancelled)
        {
            return "Ovaj zahtjev više nije moguće otkazati.";
        }

        return null;
    }
}
