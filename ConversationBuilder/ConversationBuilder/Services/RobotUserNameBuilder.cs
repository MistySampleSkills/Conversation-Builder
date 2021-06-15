namespace ConversationBuilder
{
	public static class RobotUserNameBuilder
	{
		public static string BuildUserName(string serialNumber)
		{
			return serialNumber + "@conversation-builder.com";
		}
	}
}