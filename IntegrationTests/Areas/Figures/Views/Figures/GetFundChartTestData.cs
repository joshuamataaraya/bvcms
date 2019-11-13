using System.Collections;
using System.Collections.Generic;

namespace IntegrationTests.Areas.Figures.Views.Figures
{
    public class GetFundChartTestData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            IEnumerable<int> nullArray = null;
            yield return new object[] { nullArray, null };
            yield return new object[] { new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13 }, 2019 };            
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
