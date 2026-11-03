using System.Collections.Generic;

namespace PovuciPotegni.Core
{
    public interface IAnalytics
    {
        void Track(string eventName, IReadOnlyDictionary<string, object> parameters);
    }
}
