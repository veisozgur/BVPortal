namespace BV.Domain.Quotes;

public enum QuoteRequestStatus
{
    Draft = 1,
    Submitted = 2,
    UnderReview = 3,
    Answered = 4,
    Accepted = 5,
    Rejected = 6,
    Cancelled = 7
}
