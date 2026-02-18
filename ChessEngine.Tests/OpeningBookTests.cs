using TestFrameworkCore.Assertions;
using TestFrameworkCore.Attributes;

namespace ChessEngine.Tests
{
    [TestClass(Category = "OpeningBook", Priority = 2)]
    public class OpeningBookTests
    {
        private const string ValidBookContent =
            "e2e4 e7e5 g1f3 b8c6\n" +
            "d2d4 d7d5 c2c4\n" +
            "g1f3 d7d5 g2g3";

        private const string InvalidBookContent =
            "e2e4 e7e5 g1f3 x9x9\n" +  // Некорректный ход
            "d2d4 d7d5 c2c4";

        [TestMethod]
        public async Task OpeningBook_LoadFromFileAsync_WithInvalidPath_ThrowsFileNotFoundExceptionAsync()
        {
            string invalidPath = "C:\\NonExistentFolder\\nonexistent_book.txt";

            // Assert - проверяем асинхронное исключение
            await Assert.ThrowsAsync<FileNotFoundException>(
                async () => await OpeningBook.LoadFromFileAsync(invalidPath),
                "LoadFromFileAsync should throw FileNotFoundException for non-existent file"
            );
        }

        [TestMethod]
        public async Task OpeningBook_ValidateAndLoadAsync_WithInvalidContent_ThrowsInvalidDataExceptionAsync()
        {
            // Assert - проверяем асинхронный метод, который внутри вызывает синхронную обработку
            await Assert.ThrowsAsync<InvalidDataException>(
                async () => await OpeningBook.ValidateAndLoadAsync(null, InvalidBookContent),
                "ValidateAndLoadAsync should throw InvalidDataException for invalid content"
            );
        }

        [TestMethod]
        public async Task OpeningBook_TryFindMoveAsync_WithNullPosition_ThrowsArgumentNullExceptionAsync()
        {
            // Arrange
            string tempFile = Path.GetTempFileName();
            try
            {
                File.WriteAllText(tempFile, ValidBookContent);
                var book = await OpeningBook.LoadFromFileAsync(tempFile);

                // Assert - проверяем асинхронное исключение при вызове метода с null
                await Assert.ThrowsAsync<ArgumentNullException>(
                    async () => await book.TryFindMoveAsync(null),
                    "TryFindMoveAsync should throw ArgumentNullException for null position"
                );
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [TestMethod]
        public async Task OpeningBook_ComplexAsyncScenario_WithMultipleExceptions()
        {
            // Тест, демонстрирующий несколько асинхронных операций с обработкой исключений

            string tempFile = Path.GetTempFileName();
            try
            {
                // 1. Сначала проверяем что файл не существует (должно быть исключение)
                string invalidPath = "C:\\nonexistent_" + Guid.NewGuid() + ".txt";
                await Assert.ThrowsAsync<FileNotFoundException>(
                    async () => await OpeningBook.LoadFromFileAsync(invalidPath)
                );

                // 2. Создаем файл с некорректным содержимым
                File.WriteAllText(tempFile, InvalidBookContent);

                // 3. Проверяем что загрузка файла с некорректным содержимым выбрасывает исключение
                await Assert.ThrowsAsync<InvalidDataException>(
                    async () => await OpeningBook.LoadFromFileAsync(tempFile)
                );

                // 4. Теперь создаем корректный файл
                File.WriteAllText(tempFile, ValidBookContent);

                // 5. Загружаем корректный файл - исключения быть не должно
                var book = await OpeningBook.LoadFromFileAsync(tempFile);
                Assert.IsNotNull(book);

                // 6. Проверяем поиск хода с корректной позицией
                var position = new Position("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR");
                var (move, count) = await book.TryFindMoveAsync(position);

                Assert.IsTrue(count > 0, "Should find moves");
                Assert.IsNotNull(move, "Move should not be null");

                // 7. Проверяем поиск хода с null позицией - должно быть исключение
                await Assert.ThrowsAsync<ArgumentNullException>(
                    async () => await book.TryFindMoveAsync(null)
                );
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        [TestMethod]
        public void OpeningBook_ParseSANMove_WithInvalidFormat_ThrowsFormatException()
        {
            // Создаем экземпляр через рефлексию для доступа к приватному методу
            var book = new OpeningBook(ValidBookContent, true);
            var parseMethod = typeof(OpeningBook).GetMethod("ParseSANMove",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Assert - проверяем что метод ParseSANMove выбрасывает FormatException
            Assert.Throws<FormatException>(
                () => book.ParseSANMove("e2"),
                "ParseSANMove should throw FormatException for too short move"
            );
        }
    }
}
