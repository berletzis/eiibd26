namespace eiibd26.Services
{
    // Simple abstraction para envío de SMS
    public interface ISmsSender
    {
        /// <summary>
        /// Envía un SMS al número indicado (en formato E.164 preferible) con el texto proporcionado.
        /// </summary>
        Task SendSmsAsync(string to, string body);
    }
}