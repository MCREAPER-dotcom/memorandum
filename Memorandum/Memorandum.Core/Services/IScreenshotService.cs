namespace Memorandum.Core.Services
{
    /// <summary>
    /// Захват области экрана (скриншот). Реализация в Infrastructure.
    /// </summary>
    public interface IScreenshotService
    {
        /// <summary>
        /// Захватывает указанную область экрана и возвращает изображение в виде байтов (PNG).
        /// </summary>
        /// <param name="x">Левый верхний угол по X.</param>
        /// <param name="y">Левый верхний угол по Y.</param>
        /// <param name="width">Ширина области.</param>
        /// <param name="height">Высота области.</param>
        byte[] CaptureRegion(int x, int y, int width, int height);

        /// <summary>
        /// Захватывает весь экран.
        /// </summary>
        byte[] CaptureFullScreen();
    }
}
