
using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs;

public record EnrolmentRequest(
    [Range(1, int.MaxValue)] int CategoryId
);

public record EnrolmentStatusRequest(
    [Required] string Status
);

public record EnrolmentResponse(
    int EnrolmentId,
    int ParticipantId,
    string ParticipantName,
    int EventId,
    string EventName,
    int CategoryId,
    string CategoryName,
    DateTime EnrolmentDate,
    string Status
);
