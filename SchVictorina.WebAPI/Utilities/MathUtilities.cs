using System;

namespace SchVictorina.WebAPI.Utilities
{
    public class MathUtilities
    {
        public static string GetPositiveExpression(string expression1, string expression2, char @operator)
        {
            if (expression1 == null)
                throw new ArgumentNullException("expression1");
            if (expression2 == null)
                throw new ArgumentNullException("expression2");
            if (expression2.StartsWith('-'))
            {
                if (@operator == '+' || @operator == '-')
                {
                    expression2 = expression2.TrimStart('-');
                    if (@operator == '-')
                        @operator = '+';
                    else if (@operator == '+')
                        @operator = '-';
                }
                else
                {
                    expression2 = $"({expression2})";
                }
            }
            return $"{expression1} {@operator} {expression2}";
        }
    }
}
