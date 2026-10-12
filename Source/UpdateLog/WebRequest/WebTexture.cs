using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UpdateLogTool
{
	public class WebTexture : IDisposable
	{
		public Texture2D texture;

		public WebTexture()
		{
			Status = DownloadStatus.InProgress;
		}

		public DownloadStatus Status { get; internal set; }

		public void SetTexture(Texture2D newTexture)
		{
			Dispose();
			texture = newTexture;
		}

		public void Dispose()
		{
			if (texture)
			{
				Object.Destroy(texture);
			}
		}

		public static string StringStatus(string key, DownloadStatus status)
		{
			switch (status)
			{
				case DownloadStatus.Failed:
					return $"<i>{key} failed to load.</i>";
				case DownloadStatus.InProgress:
					return $"<i>{key} loading{GenText.MarchingEllipsis(0f)}</i>";
				case DownloadStatus.Success:
					return $"<i>{key} successfully loaded.</i>";
			}
			throw new MissingMemberException(nameof(DownloadStatus));
		}
	}
}
