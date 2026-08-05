using BV.Domain.Quotes;
using Xunit;

namespace BV.UnitTests.Quotes;

public sealed class QuoteRequestTests
{
    [Fact]
    public void Submit_Should_Require_At_Least_One_Item()
    {
        var quote = new QuoteRequest(Guid.NewGuid(), QuoteRequestType.Office, "Ofis teklifi", null);

        Assert.Throws<InvalidOperationException>(() => quote.Submit());
    }

    [Fact]
    public void Submit_Should_Change_Status_When_Item_Exists()
    {
        var quote = new QuoteRequest(Guid.NewGuid(), QuoteRequestType.School, "Okul teklifi", null);
        quote.AddItem("Defter", 10, "Adet", null);

        quote.Submit();

        Assert.Equal(QuoteRequestStatus.Submitted, quote.Status);
        Assert.NotNull(quote.SubmittedAtUtc);
    }

    [Fact]
    public void Answer_Should_Change_Status_From_Submitted()
    {
        var quote = new QuoteRequest(Guid.NewGuid(), QuoteRequestType.Office, "Ofis teklifi", null);
        quote.AddItem("Kağıt", 5, "Paket", null);
        quote.Submit();

        quote.MarkAnswered();

        Assert.Equal(QuoteRequestStatus.Answered, quote.Status);
        Assert.NotNull(quote.AnsweredAtUtc);
    }
}
