using System;
using System.Text;

public static class NumberToWordConverter
{
    /// <summary>
    /// This method expose the use of the class.
    /// Supply the number  to be converted and the currency extension of your choice
    /// </summary>
    /// <param name="input">Value to be converterd e.g 5023</param>
    /// <param name="separator">Currency extensions of your choice e.g Dollar </param>
    /// <returns></returns>
    public static string GetNumberConverter(string input, string separator = "point")
    {
        if (!string.IsNullOrEmpty(input))
        {
            input = input.Replace(",", "");
            var numberSb = new StringBuilder();
            numberSb.Append(CheckForNegativity(input));
            var newInput = RemoveNegativity(input);

            if (newInput.Contains("."))
            {
                //numberSb.Append(CheckForNegativity(newInput));
                var splitInput = newInput.Split('.');
                numberSb.Append(ConvertInput(splitInput[0]));
                numberSb.Append($" {separator} ");
                var secondPart = splitInput[1].ToCharArray();
                foreach (var number in secondPart)
                {
                    numberSb.Append(ConvertTwoDigit(number.ToString()));
                    numberSb.Append(" ");
                }
            }
            else
            {
                numberSb.Append(ConvertInput(newInput));
            }
            numberSb.Append(".");

            return numberSb.ToString();
        }
        return "Please type in a value";
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="input">Value to be converterd e.g 5023</param>
    /// <param name="currency">Currency extensions of your choice e.g Dollar </param>
    /// <param name="unit"> currency extension e.g kobo or cent </param>
    /// <returns></returns>
    public static string ConverterToCurrency(string input, string currency = "Naira", string unit = "Kobo")
    {
        if (!string.IsNullOrEmpty(input))
        {
            input = input.Replace(",", "");
            var numberSb = new StringBuilder();
            numberSb.Append(CheckForNegativity(input));
            var newInput = RemoveNegativity(input);

            if (newInput.Contains("."))
            {
                var splitInput = newInput.Split('.');
                numberSb.Append(ConvertInput(splitInput[0]));
                numberSb.Append($" {currency}");

                var checkForZero = ConvertInput(splitInput[1]);
                if (!checkForZero.Equals("Zero"))
                {
                    numberSb.Append(" and ");
                    numberSb.Append(ConvertInput(splitInput[1]));
                    numberSb.Append($" {unit}");
                }

            }
            else
            {
                //numberSb.Append(CheckForNegativity(newInput));
                numberSb.Append(ConvertInput(newInput));
                numberSb.Append($" {currency}");
            }

            numberSb.Append($" only.");
            return numberSb.ToString();
        }
        return "Please type in a value";
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="input">Value to be converterd e.g 5023</param>
    /// <param name="currency">Currency extensions of your choice e.g Dollar </param>
    /// <param name="unit"> currency extension e.g kobo or cent </param>
    /// <returns></returns>
    public static string ConverterToCurrency(double doubleinput, string currency = "Naira", string unit = "Kobo")
    {
        var input = Math.Round(doubleinput, 2).ToString();
        return ConverterToCurrency(input, currency, unit);
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="input">Value to be converterd e.g 5023</param>
    /// <param name="currency">Currency extensions of your choice e.g Dollar </param>
    /// <param name="unit"> currency extension e.g kobo or cent </param>
    /// <returns></returns>
    public static string ConverterToCurrency(decimal decimalinput, string currency = "Naira", string unit = "Kobo")
    {
        var input = Math.Round(decimalinput, 2).ToString();
        return ConverterToCurrency(input, currency, unit);
    }

    private static string CheckForNegativity(string input)
    {
        if (input.StartsWith("-", System.StringComparison.CurrentCulture))
        {
            return "Minus ";
        }
        return "";
    }

    private static string RemoveNegativity(string input)
    {
        if (input.StartsWith("-"))
        {
            return input.Remove(0, 1);
        }
        return input;
    }

    private static string ConvertInput(string input)
    {
        var convertSb = new StringBuilder();
        var inputLength = input.Length;
        if (inputLength.Equals(1))
        {
            convertSb.Append(ConvertTwoDigit(input));
        }
        else if (inputLength.Equals(2))
        {
            convertSb.Append(JustTwoDigitConverter(input));
        }
        else if (inputLength.Equals(3))
        {
            convertSb.Append(ConvertToHundred(input));
        }
        else if (inputLength.Equals(4))
        {
            convertSb.Append(ConverttoOneThousand(input));
            //ouput.Append(ConvertToHundred(input));
        }
        else if (inputLength.Equals(5))
        {
            convertSb.Append(ConvertToTenThousand(input));
        }
        else if (inputLength.Equals(6))
        {
            convertSb.Append(ConvertToHundredThousand(input));
        }
        else if (inputLength.Equals(7))
        {
            convertSb.Append(ConvertToOneMillion(input));
        }
        else if (inputLength.Equals(8))
        {
            convertSb.Append(ConvertToTenMillion(input));
        }
        else if (inputLength.Equals(9))
        {
            convertSb.Append(ConvertToHundredMillion(input));
        }
        else if (inputLength.Equals(10))
        {
            convertSb.Append(ConvertToOneBillion(input));
        }
        else if (inputLength.Equals(11))
        {
            convertSb.Append(ConvertToTenBillion(input));
        }
        else if (inputLength.Equals(12))
        {
            convertSb.Append(ConvertHundredTenBillion(input));
        }
        else if (inputLength.Equals(13))
        {
            convertSb.Append(ConvertOneTrillion(input));
        }
        else if (inputLength.Equals(14))
        {
            convertSb.Append(ConvertTenTrillion(input));
        }
        else if (inputLength.Equals(15))
        {
            convertSb.Append(ConvertHundredTrillion(input));
        }
        else if (inputLength.Equals(16))
        {
            convertSb.Append(ConvertOneQuadrillion(input));
        }

        // convertSb.Append(" Naira Only");Quadrillion 
        return convertSb.ToString();
    }

    private static string ConvertOneQuadrillion(string input)
    {
        var oneQuadrillionSb = new StringBuilder();
        if (input.StartsWith("0", System.StringComparison.CurrentCulture))
        {
            var fifteenDigitOutput = RemoveZeroFromTheBegining(input);
            oneQuadrillionSb.Append(ConvertHundredTrillion(fifteenDigitOutput));
        }
        else
        {
            var firstNumber = input.Substring(0, 1);
            oneQuadrillionSb.Append($"{ConvertTwoDigit(firstNumber)} Quadrillion");
            var otherNumber = input.Substring(1, 15);
            if (otherNumber.Equals("000000000000000"))
            {
            }
            else
            {
                oneQuadrillionSb.Append(", ");
                oneQuadrillionSb.Append(ConvertHundredTrillion(otherNumber));
            }
        }
        return oneQuadrillionSb.ToString();
    }

    private static string ConvertHundredTrillion(string input)
    {
        var hundredTrillionSb = new StringBuilder();
        if (input.StartsWith("0", System.StringComparison.CurrentCulture))
        {
            var fourteenDigitOutput = RemoveZeroFromTheBegining(input);
            hundredTrillionSb.Append(ConvertTenTrillion(fourteenDigitOutput));
        }
        else
        {
            var firstNumber = input.Substring(0, 3);
            hundredTrillionSb.Append($"{ConvertToHundred(firstNumber)} Trillion");
            var otherNumber = input.Substring(3, 12);
            if (otherNumber.Equals("000000000000"))
            {
            }
            else
            {
                hundredTrillionSb.Append(", ");
                hundredTrillionSb.Append(ConvertHundredTenBillion(otherNumber));
            }
        }
        return hundredTrillionSb.ToString();
    }

    private static string ConvertTenTrillion(string input)
    {
        var tenTrillionSb = new StringBuilder();
        if (input.StartsWith("0", System.StringComparison.CurrentCulture))
        {
            var thirteenDigitOutput = RemoveZeroFromTheBegining(input);
            tenTrillionSb.Append(ConvertOneTrillion(thirteenDigitOutput));
        }
        else
        {
            var firstNumber = input.Substring(0, 2);
            tenTrillionSb.Append($"{JustTwoDigitConverter(firstNumber)} Trillion");
            var otherNumber = input.Substring(2, 12);
            if (otherNumber.Equals("000000000000"))
            {
            }
            else
            {
                tenTrillionSb.Append(", ");
                tenTrillionSb.Append(ConvertHundredTenBillion(otherNumber));
            }
        }
        return tenTrillionSb.ToString();
    }
    private static string ConvertOneTrillion(string input)
    {
        var oneTrillionSb = new StringBuilder();
        if (input.StartsWith("0", System.StringComparison.CurrentCulture))
        {
            var twelveDigitOutput = RemoveZeroFromTheBegining(input);
            oneTrillionSb.Append(ConvertHundredTenBillion(twelveDigitOutput));
        }
        else
        {
            var firstNumber = input.Substring(0, 1);
            oneTrillionSb.Append($"{ConvertTwoDigit(firstNumber)} Trillion");
            var otherNumber = input.Substring(1, 12);
            if (otherNumber.Equals("000000000000"))
            {
            }
            else
            {
                oneTrillionSb.Append(", ");
                oneTrillionSb.Append(ConvertHundredTenBillion(otherNumber));
            }
        }
        return oneTrillionSb.ToString();
    }

    private static string ConvertHundredTenBillion(string input)
    {
        var hundredBillionSb = new StringBuilder();
        if (input.StartsWith("0", System.StringComparison.CurrentCulture))
        {
            var elevenDigitOutput = RemoveZeroFromTheBegining(input);
            hundredBillionSb.Append(ConvertToTenBillion(elevenDigitOutput));
        }
        else
        {
            var firstNumber = input.Substring(0, 3);
            hundredBillionSb.Append($"{ConvertToHundred(firstNumber)} Billion");
            var otherNumber = input.Substring(3, 9);
            if (otherNumber.Equals("000000000"))
            {
            }
            else
            {
                hundredBillionSb.Append(", ");
                hundredBillionSb.Append(ConvertToHundredMillion(otherNumber));
            }
        }
        return hundredBillionSb.ToString();
    }
    private static string ConvertToTenBillion(string input)
    {
        var tenBillionSb = new StringBuilder();
        if (input.StartsWith("0", System.StringComparison.CurrentCulture))
        {
            var tenDigitOutput = RemoveZeroFromTheBegining(input);
            tenBillionSb.Append(ConvertToOneBillion(tenDigitOutput));
        }
        else
        {
            var firstNumber = input.Substring(0, 2);
            tenBillionSb.Append($"{JustTwoDigitConverter(firstNumber)} Billion");
            var otherNumber = input.Substring(2, 9);
            if (otherNumber.Equals("000000000"))
            {
            }
            else
            {
                tenBillionSb.Append(", ");
                tenBillionSb.Append(ConvertToHundredMillion(otherNumber));
            }
        }
        return tenBillionSb.ToString();
    }

    private static string ConvertToOneBillion(string input)
    {
        var oneBillionSb = new StringBuilder();
        if (input.StartsWith("0", System.StringComparison.CurrentCulture))
        {
            var nineDigitOutput = RemoveZeroFromTheBegining(input);
            oneBillionSb.Append(ConvertToHundredMillion(nineDigitOutput));
        }
        else
        {
            var firstNumber = input.Substring(0, 1);
            oneBillionSb.Append($"{ConvertTwoDigit(firstNumber)} Billion");
            var otherNumber = input.Substring(1, 9);
            if (otherNumber.Equals("000000000"))
            {
            }
            else
            {
                oneBillionSb.Append(", ");
                oneBillionSb.Append(ConvertToHundredMillion(otherNumber));
            }
        }
        return oneBillionSb.ToString();
    }

    private static string ConvertToHundredMillion(string input)
    {
        var hundredMillionSb = new StringBuilder();
        if (input.StartsWith("0"))
        {
            var eightDigitOutput = RemoveZeroFromTheBegining(input);
            hundredMillionSb.Append(ConvertToTenMillion(eightDigitOutput));
        }
        else
        {
            var firstNumber = input.Substring(0, 3);
            hundredMillionSb.Append($"{ConvertToHundred(firstNumber)} Million");
            var otherNumber = input.Substring(3, 6);
            if (otherNumber.Equals("000000"))
            {
            }
            else
            {
                hundredMillionSb.Append(", ");
                hundredMillionSb.Append(ConvertToHundredThousand(otherNumber));
            }
            //ouput.Append(ConvertToHundred(input));
        }
        return hundredMillionSb.ToString();
    }

    private static string ConvertToTenMillion(string input)
    {
        var tenMillionSb = new StringBuilder();
        if (input.StartsWith("0"))
        {
            var sevenDigitOutput = RemoveZeroFromTheBegining(input);
            tenMillionSb.Append(ConvertToOneMillion(sevenDigitOutput));
        }
        else
        {
            var firstNumber = input.Substring(0, 2);
            tenMillionSb.Append($"{JustTwoDigitConverter(firstNumber)} Million");
            var otherNumber = input.Substring(2, 6);
            if (otherNumber.Equals("000000"))
            {
            }
            else
            {
                tenMillionSb.Append(", ");
                tenMillionSb.Append(ConvertToHundredThousand(otherNumber));
            }
            //ouput.Append(ConvertToHundred(input));
        }
        return tenMillionSb.ToString();
    }

    private static string ConvertToOneMillion(string input)
    {
        var oneMillionSb = new StringBuilder();
        if (input.StartsWith("0"))
        {
            var sixDigitOutput = RemoveZeroFromTheBegining(input);
            oneMillionSb.Append(ConvertToHundredThousand(sixDigitOutput));
        }
        else
        {
            var firstNumber = input.Substring(0, 1);
            oneMillionSb.Append($"{ConvertTwoDigit(firstNumber)} Million");
            var otherNumber = input.Substring(1, 6);
            if (otherNumber.Equals("000000"))
            {
            }
            else
            {
                oneMillionSb.Append(", ");
                oneMillionSb.Append(ConvertToHundredThousand(otherNumber));
            }
            //ouput.Append(ConvertToHundred(input));
        }
        return oneMillionSb.ToString();
    }

    private static string ConvertToHundredThousand(string input)
    {
        var hundredthousandSb = new StringBuilder();
        if (input.StartsWith("0"))
        {
            var fiveDigitOutput = RemoveZeroFromTheBegining(input);
            hundredthousandSb.Append(ConvertToTenThousand(fiveDigitOutput));
        }
        else
        {
            var firstNumber = input.Substring(0, 3);
            hundredthousandSb.Append($"{ConvertToHundred(firstNumber)} Thousand");
            var otherNumber = input.Substring(3, 3);
            if (otherNumber.Equals("000"))
            {
            }
            else
            {
                hundredthousandSb.Append(", ");
                hundredthousandSb.Append(ConvertToHundred(otherNumber));
            }
            //ouput.Append(ConvertToHundred(input));
        }
        return hundredthousandSb.ToString();
    }

    private static string ConvertToTenThousand(string input)
    {
        var tenThousandStringBuilder = new StringBuilder();
        if (input.StartsWith("0"))
        {
            var fourDigitOutput = RemoveZeroFromTheBegining(input);
            tenThousandStringBuilder.Append(ConverttoOneThousand(fourDigitOutput));
        }
        else
        {
            var firstNumber = input.Substring(0, 2);
            tenThousandStringBuilder.Append($"{JustTwoDigitConverter(firstNumber)} Thousand");
            var otherNumber = input.Substring(2, 3);
            if (otherNumber.Equals("000"))
            {
            }
            else
            {
                //tenThousandStringBuilder.Append(" and ");
                tenThousandStringBuilder.Append(" ");
                tenThousandStringBuilder.Append(ConvertToHundred(otherNumber));
            }
        }
        return tenThousandStringBuilder.ToString();
    }

    private static string ConverttoOneThousand(string input)
    {
        var oneThousandStringBulider = new StringBuilder();
        if (input.StartsWith("0"))
        {
            var threeDigitOutput = RemoveZeroFromTheBegining(input);
            oneThousandStringBulider.Append(ConvertToHundred(threeDigitOutput));
        }
        else
        {
            var firstNumber = input.Substring(0, 1);
            oneThousandStringBulider.Append($"{ConvertTwoDigit(firstNumber)} Thousand");
            var otherNumber = input.Substring(1, 3);
            if (otherNumber.Equals("000"))
            {
            }
            else
            {
                oneThousandStringBulider.Append(" and ");
                oneThousandStringBulider.Append(ConvertToHundred(otherNumber));
            }
        }

        return oneThousandStringBulider.ToString();
    }

    private static string ConvertToHundred(string input)
    {
        var hundredStringBuilder = new StringBuilder();
        if (input.StartsWith("0"))
        {
            var twoDigitOutput = RemoveZeroFromTheBegining(input);
            hundredStringBuilder.Append(JustTwoDigitConverter(twoDigitOutput));
        }
        else
        {
            var firstNumber = input.Substring(0, 1);
            hundredStringBuilder.Append($"{ConvertTwoDigit(firstNumber)} Hundred");
            var otherNumber = input.Substring(1, 2);
            if (otherNumber.Equals("00"))
            {
            }
            else
            {
                //hundredStringBuilder.Append(" and ");
                hundredStringBuilder.Append(" ");
                hundredStringBuilder.Append(JustTwoDigitConverter(otherNumber));
            }
        }
        return hundredStringBuilder.ToString();
    }

    private static string JustTwoDigitConverter(string twoInput)
    {
        var twoDigitSb = new StringBuilder();
        //string twoDigitOutput = string.Empty;

        if (twoInput.StartsWith("0", System.StringComparison.CurrentCulture))
        {
            var twoDigitOutput = RemoveZeroFromTheBegining(twoInput);
            twoDigitSb.Append(ConvertTwoDigit(twoDigitOutput));
        }
        else if (twoInput.Substring(0, 1) != "1")
        {
            var spiltInput = twoInput.ToCharArray();
            for (int i = 0; i < spiltInput.Length; i++)
            {
                if (i == 0)
                {
                    twoDigitSb.Append(ConvertTwoDigit($"{spiltInput[i]}0"));
                }
                else if (spiltInput[i].Equals('0'))
                {
                    ;
                }
                else
                {
                    twoDigitSb.Append(" ");
                    twoDigitSb.Append(ConvertTwoDigit($"{spiltInput[i]}"));
                }
                //twoDigitStringBuuilder.Append(i == 0
                //    ? $"{ConvertTwoDigit($"{spiltInput[i]}0")} - "
                //    : ConvertTwoDigit($"{spiltInput[i]}"));
            }
        }
        else
        {
            twoDigitSb.Append(ConvertTwoDigit(twoInput));
        }

        return twoDigitSb.ToString();
    }

    private static string RemoveZeroFromTheBegining(string input) => input.Remove(0, 1);

    private static string ConvertTwoDigit(string num)
    {
        switch (num)
        {
            case "0":
                return "Zero";

            case "1":
                return "One";

            case "2":
                return "Two";

            case "3":
                return "Three";

            case "4":
                return "Four";

            case "5":
                return "Five";

            case "6":
                return "Six";

            case "7":
                return "Seven";

            case "8":
                return "Eight";

            case "9":
                return "Nine";

            case "10":
                return "Ten";

            case "11":
                return "Eleven";

            case "12":
                return "Twelve";

            case "13":
                return "Thirteen";

            case "14":
                return "Fourteen";

            case "15":
                return "Fifteen";

            case "16":
                return "Sixteen";

            case "17":
                return "Seventeen";

            case "18":
                return "Eighteen";

            case "19":
                return "Nineteen";

            case "20":
                return "Twenty";

            case "30":
                return "Thirty";

            case "40":
                return "Forty";

            case "50":
                return "Fifty";

            case "60":
                return "Sixty";

            case "70":
                return "Seventy";

            case "80":
                return "Eighty";

            case "90":
                return "Ninety";

            default:
                return "";
        }
    }
}

