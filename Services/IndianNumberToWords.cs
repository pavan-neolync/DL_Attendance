namespace DLAttendance.Services;

public static class IndianNumberToWords
{
    private static readonly string[] Units =
    [
        "ZERO", "ONE", "TWO", "THREE", "FOUR", "FIVE", "SIX", "SEVEN", "EIGHT", "NINE",
        "TEN", "ELEVEN", "TWELVE", "THIRTEEN", "FOURTEEN", "FIFTEEN", "SIXTEEN",
        "SEVENTEEN", "EIGHTEEN", "NINETEEN"
    ];

    private static readonly string[] Tens =
    [
        "", "", "TWENTY", "THIRTY", "FORTY", "FIFTY", "SIXTY", "SEVENTY", "EIGHTY", "NINETY"
    ];

    public static string Convert(decimal amount)
    {
        var rupees = (long)Math.Floor(amount);
        if (rupees == 0)
        {
            return "RUPEES ZERO ONLY";
        }

        return $"RUPEES {ConvertNumber(rupees)} ONLY";
    }

    private static string ConvertNumber(long number)
    {
        if (number < 20)
        {
            return Units[number];
        }

        if (number < 100)
        {
            return $"{Tens[number / 10]}{(number % 10 == 0 ? "" : " " + Units[number % 10])}";
        }

        if (number < 1000)
        {
            return $"{Units[number / 100]} HUNDRED{(number % 100 == 0 ? "" : " " + ConvertNumber(number % 100))}";
        }

        if (number < 100000)
        {
            return $"{ConvertNumber(number / 1000)} THOUSAND{(number % 1000 == 0 ? "" : " " + ConvertNumber(number % 1000))}";
        }

        if (number < 10000000)
        {
            return $"{ConvertNumber(number / 100000)} LAKH{(number % 100000 == 0 ? "" : " " + ConvertNumber(number % 100000))}";
        }

        return $"{ConvertNumber(number / 10000000)} CRORE{(number % 10000000 == 0 ? "" : " " + ConvertNumber(number % 10000000))}";
    }
}
