

namespace App.Domain.Domain.Logging
{
    /// <summary>
    /// Các hành động có thể xảy ra trong một lượt thi.
    /// </summary>
    public enum ExamActivityAction
    {
        Start = 1,
        Answer = 2,
        ChangeAnswer = 3,
        ClearAnswer = 4,
        Navigate = 5,
        Pause = 6,
        Resume = 7,
        Submit = 8,
        SubmitSuccess = 9,
        SubmitFailed = 10,
        Timeout = 11,
        Disconnect = 12,
        Reconnect = 13
    }
}
