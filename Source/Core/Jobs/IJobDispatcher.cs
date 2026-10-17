namespace CoreLib.Jobs
{
	public interface IJobDispatcher
	{
		/// <summary>
		/// Update the dispatcher to check or complete scheduled jobs.
		/// </summary>
		void LateUpdate();

		/// <summary>
		/// Signal from root dispatcher to complete now as it is pending destruction.
		/// </summary>
		void OnDestroy();
	}
}
