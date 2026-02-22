using TestFrameworkCore.Assertions;
using TestFrameworkCore.Attributes;

namespace ChessEngine.Tests
{
    [TestClass(Category = "OpeningBook")]
    public class OpeningBookTests
    {
        private const string ValidBookContent =
            "e2e4 e7e5 g1f3 b8c6\n" +
            "d2d4 d7d5 c2c4\n" +
            "g1f3 d7d5 g2g3";

        private const string InvalidBookContent =
            "e2e4 e7e5 g1f3 x9x9\n" +  // Некорректный ход
            "d2d4 d7d5 c2c4";

        [TestMethod(Priority = 1)]
        public async Task OpeningBook_LoadFromFileAsync_WithInvalidPath_ThrowsFileNotFoundExceptionAsync()
        {
            string invalidPath = "C:\\NonExistentFolder\\nonexistent_book.txt";

            await Assert.ThrowsAsync<FileNotFoundException>(
                async () => await OpeningBook.LoadFromFileAsync(invalidPath),
                "LoadFromFileAsync should throw FileNotFoundException for non-existent file"
            );
        }

        [TestMethod(Priority = 2)]
        public async Task OpeningBook_ValidateAndLoadAsync_WithInvalidContent_ThrowsInvalidDataExceptionAsync()
        {
            await Assert.ThrowsAsync<InvalidDataException>(
                async () => await OpeningBook.ValidateAndLoadAsync(null, InvalidBookContent),
                "ValidateAndLoadAsync should throw InvalidDataException for invalid content"
            );
        }

        [TestMethod(Priority = 2)]
        public async Task OpeningBook_TryFindMoveAsync_WithNullPosition_ThrowsArgumentNullExceptionAsync()
        {
            // Arrange
            string tempFile = Path.GetTempFileName();
            try
            {
                File.WriteAllText(tempFile, ValidBookContent);
                var book = await OpeningBook.LoadFromFileAsync(tempFile);

                // Assert
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

        [TestMethod(Priority = 3)]
        public async Task OpeningBook_ComplexAsyncScenario_WithMultipleExceptions()
        {
            string tempFile = Path.GetTempFileName();
            try
            {
                string invalidPath = "C:\\nonexistent_" + Guid.NewGuid() + ".txt";
                await Assert.ThrowsAsync<FileNotFoundException>(
                    async () => await OpeningBook.LoadFromFileAsync(invalidPath)
                );

                File.WriteAllText(tempFile, InvalidBookContent);

                await Assert.ThrowsAsync<InvalidDataException>(
                    async () => await OpeningBook.LoadFromFileAsync(tempFile)
                );

                File.WriteAllText(tempFile, ValidBookContent);

                var book = await OpeningBook.LoadFromFileAsync(tempFile);
                Assert.IsNotNull(book);

                var position = new Position("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR");
                var (move, count) = await book.TryFindMoveAsync(position);

                Assert.IsTrue(count > 0, "Should find moves");
                Assert.IsNotNull(move, "Move should not be null");

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
    }
}
