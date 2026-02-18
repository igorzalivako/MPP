namespace ChessEngine
{
    public class OpeningBook
    {
        private List<List<Move>> _moves = new List<List<Move>>();

        /*public OpeningBook(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("Opening book not found", path);

            foreach (var line in File.ReadLines(path))
            {
                var gameMoves = new List<Move>();
                var position = new Position("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR");

                foreach (var sanMove in line.Split(' ').Where(m => !string.IsNullOrWhiteSpace(m)))
                {
                    var (from, to) = ParseSANMove(sanMove);
                    var legalMoves = LegalMovesGenerator.Generate(position, position.ActiveColor, false);

                    var foundMove = legalMoves.FirstOrDefault((Move m) => m.From == from && m.To == to);
                    if (foundMove is null)
                        throw new InvalidDataException($"Некорректный ход {sanMove} в дебютной базе");
                    gameMoves.Add(foundMove.Value);
                    position.MakeMove(foundMove.Value);
                }
                _moves.Add(gameMoves);
            }
        }*/

        // Существующий конструктор для работы с файлами по пути
        public OpeningBook(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("Opening book not found", path);

            ProcessContent(File.ReadLines(path));
        }

        // Новый конструктор для работы с содержимым из строки
        public OpeningBook(string content, bool isContent)
        {
            var lines = content.Split(
                new[] { "\r\n", "\r", "\n" },
                StringSplitOptions.RemoveEmptyEntries
            );
            ProcessContent(lines);
        }

        private void ProcessContent(IEnumerable<string> lines)
        {
            foreach (var line in lines)
            {
                var gameMoves = new List<Move>();
                var position = new Position("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR");

                foreach (var sanMove in line.Split(' ').Where(m => !string.IsNullOrWhiteSpace(m)))
                {
                    var (from, to) = ParseSANMove(sanMove);
                    var legalMoves = LegalMovesGenerator.Generate(position, position.ActiveColor, false);

                    var foundMove = legalMoves.FirstOrDefault(m => m.From == from && m.To == to);
                    if (foundMove is null)
                        throw new InvalidDataException($"Некорректный ход {sanMove} в дебютной базе");

                    gameMoves.Add(foundMove.Value);
                    position.MakeMove(foundMove.Value);
                }
                _moves.Add(gameMoves);
            }
        }

        public (Move Move, int Count) TryFindMove(Position position)
        {
            var candidateMoves = new HashSet<Move>();

            foreach (var game in _moves)
            {
                var testPosition = new Position("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR");

                for (int i = 0; i < game.Count; i++)
                {
                    if (testPosition == position)
                    {
                        if (i < game.Count - 1 && !candidateMoves.Contains(game[i]))
                            candidateMoves.Add(game[i]);
                    }

                    if (i < game.Count)
                        testPosition.MakeMove(game[i]);
                }
            }

            return candidateMoves.Count == 0
                ? (default, 0)
                : (candidateMoves.ElementAt(new Random().Next(candidateMoves.Count)), candidateMoves.Count);
        }

        public (byte from, byte to) ParseSANMove(string san)
        {
            // Пример: "e2e4" -> (12, 28)
            if (san.Length < 4) throw new FormatException($"Invalid SAN move {san}");

            int fromFile = san[0] - 'a';
            int fromRank = san[1] - '1';
            int toFile = san[2] - 'a';
            int toRank = san[3] - '1';

            return ((byte)(fromRank * 8 + fromFile), (byte)(toRank * 8 + toFile));
        }

        public static async Task<OpeningBook> LoadFromFileAsync(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"Opening book file not found: {path}");

            var lines = await File.ReadAllLinesAsync(path);
            var book = new OpeningBook();
            book.ProcessContent(lines);
            return book;
        }

        private OpeningBook() { }

        public static async Task<OpeningBook> ValidateAndLoadAsync(string path, string content = null)
        {
            if (!string.IsNullOrEmpty(path))
            {
                return await LoadFromFileAsync(path);
            }
            else if (!string.IsNullOrEmpty(content))
            {
                return await Task.Run(() => new OpeningBook(content, true));
            }
            else
            {
                throw new ArgumentException("Either path or content must be provided");
            }
        }

        public async Task<(Move Move, int Count)> TryFindMoveAsync(Position position)
        {
            if (position is null)
                throw new ArgumentNullException(nameof(position));

            return await Task.Run(() => TryFindMove(position));
        }

    }
}
