using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Extensions.Data
{
    public class PrivateSetterContractResolver : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(System.Reflection.MemberInfo member,
            MemberSerialization memberSerialization)
        {
            JsonProperty property = base.CreateProperty(member, memberSerialization);

            if (!property.Writable && member is System.Reflection.PropertyInfo propInfo)
            {
                System.Reflection.MethodInfo setter = propInfo.GetSetMethod(true);
                if (setter != null)
                {
                    property.Writable = true;
                }
            }

            return property;
        }
    }
}