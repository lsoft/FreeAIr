using System.Security;

namespace WpfHelpers
{
    /// <summary>
    /// Потребитель пароля
    /// </summary>
    public interface IConsumePassword
    {
        /// <summary>
        /// Установить пароль тому, кто его потребляет из того места, где он создается
        /// </summary>
        /// <param name="password">Пароль</param>
        void SetPassword(SecureString password);
    }
}