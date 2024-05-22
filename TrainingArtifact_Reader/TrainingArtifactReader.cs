using System;
using Kaitai;

namespace TrainingArtifactReaderNS
{
	public class TrainingArtifactReader
	{
		public TrainingArtifact FromFile(String fileName)
		{
			return TrainingArtifact.FromFile(fileName);
		}

		public TrainingArtifact FromBytes(byte[] bytes)
		{
			KaitaiStream ktStream = new KaitaiStream(bytes);
			return new TrainingArtifact(ktStream);
		}
	}
}
