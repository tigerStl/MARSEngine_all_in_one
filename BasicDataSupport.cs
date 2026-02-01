
using System;
using 
namespace Mars.Inter.MQCenter.DataLayer.network.ErrorCheckData
{

	[Serializable]
	public class MarsErrorCheckData
	{
		public string APPLICATION { get; set; }
		//public List<string> KEYWORDS { get; set; }
		public List<MarsError_Objects> Error_Objects { get; set; }
		public bool IsEnabled { get; set; }
		public bool IsIgnoreIfException { get; set; }
	}
}