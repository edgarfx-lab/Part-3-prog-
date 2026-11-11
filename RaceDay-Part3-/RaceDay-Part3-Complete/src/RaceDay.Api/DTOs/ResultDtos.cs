using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs;
public record ResultRequest(
    TimeSpan? FinishTime,
    [property: Range(1, int.MaxValue)] int? FinishPosition,
    [property: Range(1, int.MaxValue)] int? TotalFinishers);
public record ResultResponse(int ResultId, int EnrolmentId, string EventName, DateTime EventDate, string CategoryName, TimeSpan? FinishTime, int? FinishPosition, int? TotalFinishers);
