namespace TrainingApp.BLL.Auth
{
    public enum ChangePasswordStatus
    {
        Success,
        InvalidCurrentPassword,
        NewPasswordIsDefault,
        OperatorNotFound
    }
}
