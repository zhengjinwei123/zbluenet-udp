using LitJson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

class JsonHelper
{
	public static string SerializeObject(object o)
	{
		JsonMapper.RegisterExporter<float>((obj, writer) => writer.Write(Convert.ToDouble(obj)));
		JsonMapper.RegisterImporter<double, float>(input => Convert.ToSingle(input));

		string json = JsonMapper.ToJson(o);
		return json;
	}
}