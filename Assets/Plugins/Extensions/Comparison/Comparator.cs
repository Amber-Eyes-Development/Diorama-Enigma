namespace Extensions.Types
{
    /// <summary>
    /// Хелпер-сравнитель
    /// </summary>
    public static class Comparator
    {
        /// <summary>
        /// Проверка выполнения условия сравнения
        /// </summary>
        public static bool IsConditionPassed(float value, ComparisonType comparisonType, float compareTo)
        {
            switch (comparisonType)
            {
                case ComparisonType.Greater:
                    return value > compareTo;
                
                case ComparisonType.GreaterOrEqual:
                    return value >= compareTo;
                
                case ComparisonType.Less:
                    return value < compareTo;
                
                case ComparisonType.LessOrEqual:
                    return value <= compareTo;
                
                case ComparisonType.Equal:
                    return value == compareTo;
                
                case ComparisonType.NotEqual:
                    return value != compareTo;
            }

            return false;
        }

        /// <summary>
        /// Проверка выполнения условия сравнения
        /// </summary>
        public static bool IsConditionPassed(int value, ComparisonType comparisonType, int compareTo)
        {
            switch (comparisonType)
            {
                case ComparisonType.Greater:
                    return value > compareTo;

                case ComparisonType.GreaterOrEqual:
                    return value >= compareTo;

                case ComparisonType.Less:
                    return value < compareTo;

                case ComparisonType.LessOrEqual:
                    return value <= compareTo;

                case ComparisonType.Equal:
                    return value == compareTo;

                case ComparisonType.NotEqual:
                    return value != compareTo;
            }

            return false;
        }
    }
}