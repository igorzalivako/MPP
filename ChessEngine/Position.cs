namespace ChessEngine
{
    using System;
    using System.Numerics;

    public class Position
    {
        private const byte ROOK_A1 = 0;
        private const byte KING_E1 = 4;
        private const byte ROOK_H1 = 7;
        private const byte ROOK_A8 = 56;
        private const byte KING_E8 = 60;
        private const byte ROOK_H8 = 63;

        public Pieces Pieces { get; private set; }
        public byte EnPassant { get; private set; }
        public bool WhiteLongCastling { get; private set; }
        public bool WhiteShortCastling { get; private set; }
        public bool BlackLongCastling { get; private set; }
        public bool BlackShortCastling { get; private set; }
        public bool WhiteCastlingHappened { get; private set; }
        public bool BlackCastlingHappened { get; private set; }
        public float MoveCounter { get; private set; }
        public ZobristHash Hash { get; private set; }
        public PieceColor ActiveColor { get => GetActiveColor(); }
        public CastlingRights CastlingAvailability { get => GetCastlingAvailability(); }
        public RepetitionHistory RepetitionHistory { get; } = new RepetitionHistory();
        public int FiftyMovesCounter { get; private set; }

        private CastlingRights GetCastlingAvailability()
        {
            CastlingRights castlingRights = CastlingRights.None;
            if (WhiteLongCastling)
            {
                castlingRights |= CastlingRights.WhiteLong;
            }
            if (WhiteShortCastling)
            {
                castlingRights |= CastlingRights.WhiteShort;
            }
            if (BlackLongCastling)
            {
                castlingRights |= CastlingRights.BlackLong;
            }
            if (BlackShortCastling)
            {
                castlingRights |= CastlingRights.BlackShort;
            }
            return castlingRights;
        }

        private PieceColor GetActiveColor()
        {
            if (MoveCounter - Math.Floor(MoveCounter) > 1e-7)
            {
                return PieceColor.Black;
            }
            else
            {
                return PieceColor.White;
            }
        }

        public Position(string shortFen, byte enPassant = 255,
                       bool wLCastling = true, bool wSCastling = true,
                       bool bLCastling = true, bool bSCastling = true,
                       float moveCounter = 1)
        {
            Pieces = new Pieces(shortFen);
            EnPassant = enPassant;
            WhiteLongCastling = wLCastling;
            WhiteShortCastling = wSCastling;
            BlackLongCastling = bLCastling;
            BlackShortCastling = bSCastling;
            MoveCounter = moveCounter;
            Hash = new ZobristHash(Pieces, (MoveCounter - (int)MoveCounter > 1e-4f),
                                 WhiteLongCastling, WhiteShortCastling,
                                 BlackLongCastling, BlackShortCastling);
            RepetitionHistory.AddPosition(Hash);
            FiftyMovesCounter = 0;
        }

        public Position(Pieces pieces, byte enPassant, bool whiteLongCastling, bool whiteShortCastling, bool blackLongCastling, bool blackShortCastling, bool whiteCastlingHappened, bool blackCastlingHappened, float moveCounter, ZobristHash hash, RepetitionHistory repetitionHistory, int fiftyMovesCounter)
        {
            Pieces = pieces;
            EnPassant = enPassant;
            WhiteLongCastling = whiteLongCastling;
            WhiteShortCastling = whiteShortCastling;
            BlackLongCastling = blackLongCastling;
            BlackShortCastling = blackShortCastling;
            WhiteCastlingHappened = whiteCastlingHappened;
            BlackCastlingHappened = blackCastlingHappened;
            MoveCounter = moveCounter;
            Hash = hash.Clone();
            RepetitionHistory = repetitionHistory.Clone();
            FiftyMovesCounter = fiftyMovesCounter;
        }

        public void AddPiece(byte square, PieceType type, PieceColor side)
        {
            if ((Pieces.PieceBitboards[(int)side, (int)type].Value & (1UL << square)) == 0)
            {
                Pieces.PieceBitboards[(int)side, (int)type].Value |= 1UL << square;
                Hash.InvertPiece(square, type, side);
                Pieces.UpdateBitboards();
            }
        }

        public void RemovePiece(byte square, PieceType type, PieceColor side)
        {
            if ((Pieces.PieceBitboards[(int)side, (int)type].Value & (1UL << square)) != 0)
            {
                Pieces.PieceBitboards[(int)side, (int)type].Value &= ~(1UL << square);
                Hash.InvertPiece(square, type, side);
                Pieces.UpdateBitboards();
            }
        }

        public void ChangeEnPassant(byte enPassant)
        {
            EnPassant = enPassant;
        }

        public void RemoveWhiteLongCastling()
        {
            if (WhiteLongCastling)
            {
                WhiteLongCastling = false;
                Hash.InvertWhiteLongCastling();
            }
        }

        public void RemoveWhiteShortCastling()
        {
            if (WhiteShortCastling)
            {
                WhiteShortCastling = false;
                Hash.InvertWhiteShortCastling();
            }
        }

        public void RemoveBlackLongCastling()
        {
            if (BlackLongCastling)
            {
                BlackLongCastling = false;
                Hash.InvertBlackLongCastling();
            }
        }

        public void RemoveBlackShortCastling()
        {
            if (BlackShortCastling)
            {
                BlackShortCastling = false;
                Hash.InvertBlackShortCastling();
            }
        }

        public void UpdateMoveCounter()
        {
            MoveCounter += 0.5f;
            Hash.InvertMove();
        }

        public void UpdateFiftyMovesCounter(bool breakEvent)
        {
            if (breakEvent)
                FiftyMovesCounter = 0;
            else
                FiftyMovesCounter += 1;
        }

        public void MakeMove(Move move)
        {
            bool isBreakEvent = (Pieces.PieceBitboards[(int)move.AttackerSide, (int)PieceType.Pawn].Value & (1UL << move.From)) != 0
                              || move.DefenderType != PieceType.None;

            RemovePiece(move.From, move.AttackerType, move.AttackerSide);
            AddPiece(move.To, move.AttackerType, move.AttackerSide);
            if (move.DefenderType != PieceType.None)
                RemovePiece(move.To, move.DefenderType, move.DefenderSide);

            if (isBreakEvent)
                RepetitionHistory.Clear();
            RepetitionHistory.AddPosition(Hash);

            UpdateFiftyMovesCounter(isBreakEvent);

            switch (move.Flag)
            {
                case MoveFlag.Default:
                    break;

                case MoveFlag.PawnLongMove:
                    ChangeEnPassant((byte)((move.From + move.To) / 2));
                    break;

                case MoveFlag.EnPassantCapture:
                    if (move.AttackerSide == PieceColor.White)
                        RemovePiece((byte)(move.To - 8), PieceType.Pawn, PieceColor.Black);
                    else
                        RemovePiece((byte)(move.To + 8), PieceType.Pawn, PieceColor.White);
                    break;

                case MoveFlag.WhiteLongCastling:
                    RemovePiece(ROOK_A1, PieceType.Rook, PieceColor.White);
                    AddPiece(3, PieceType.Rook, PieceColor.White);
                    WhiteCastlingHappened = true;
                    break;

                case MoveFlag.WhiteShortCastling:
                    RemovePiece(ROOK_H1, PieceType.Rook, PieceColor.White);
                    AddPiece(5, PieceType.Rook, PieceColor.White);
                    WhiteCastlingHappened = true;
                    break;

                case MoveFlag.BlackLongCastling:
                    RemovePiece(ROOK_A8, PieceType.Rook, PieceColor.Black);
                    AddPiece(59, PieceType.Rook, PieceColor.Black);
                    BlackCastlingHappened = true;
                    break;

                case MoveFlag.BlackShortCastling:
                    RemovePiece(ROOK_H8, PieceType.Rook, PieceColor.Black);
                    AddPiece(61, PieceType.Rook, PieceColor.Black);
                    BlackCastlingHappened = true;
                    break;

                case MoveFlag.PromoteToKnight:
                    RemovePiece(move.To, PieceType.Pawn, move.AttackerSide);
                    AddPiece(move.To, PieceType.Knight, move.AttackerSide);
                    break;

                case MoveFlag.PromoteToBishop:
                    RemovePiece(move.To, PieceType.Pawn, move.AttackerSide);
                    AddPiece(move.To, PieceType.Bishop, move.AttackerSide);
                    break;

                case MoveFlag.PromoteToRook:
                    RemovePiece(move.To, PieceType.Pawn, move.AttackerSide);
                    AddPiece(move.To, PieceType.Rook, move.AttackerSide);
                    break;

                case MoveFlag.PromoteToQueen:
                    RemovePiece(move.To, PieceType.Pawn, move.AttackerSide);
                    AddPiece(move.To, PieceType.Queen, move.AttackerSide);
                    break;
            }

            Pieces.UpdateBitboards();

            if (move.Flag != MoveFlag.PawnLongMove)
                ChangeEnPassant(255);

            switch (move.From)
            {
                case ROOK_A1:
                    RemoveWhiteLongCastling();
                    break;
                case KING_E1:
                    RemoveWhiteLongCastling();
                    RemoveWhiteShortCastling();
                    break;
                case ROOK_H1:
                    RemoveWhiteShortCastling();
                    break;
                case ROOK_A8:
                    RemoveBlackLongCastling();
                    break;
                case KING_E8:
                    RemoveBlackLongCastling();
                    RemoveBlackShortCastling();
                    break;
                case ROOK_H8:
                    RemoveBlackShortCastling();
                    break;
            }

            MoveCounter += 0.5f;
        }

        public Position Clone()
        {
            Position result = new Position(Pieces.Clone(), EnPassant, WhiteLongCastling, WhiteShortCastling, BlackLongCastling, BlackShortCastling, WhiteCastlingHappened, BlackCastlingHappened, MoveCounter, Hash, RepetitionHistory, FiftyMovesCounter);
            return result;
        }

        public static bool operator ==(Position position1, Position position2)
        {
            return position1.EnPassant == position2.EnPassant && position1.Pieces == position2.Pieces
                && position1.WhiteLongCastling == position2.WhiteLongCastling && position1.WhiteShortCastling == position2.WhiteShortCastling
                && position1.BlackLongCastling == position2.BlackLongCastling && position1.BlackShortCastling == position2.BlackShortCastling
                && position1.WhiteCastlingHappened == position2.WhiteCastlingHappened && position1.BlackCastlingHappened == position2.BlackCastlingHappened
                && position1.MoveCounter == position2.MoveCounter && position1.FiftyMovesCounter == position2.FiftyMovesCounter;
        }

        public static bool operator !=(Position position1, Position position2)
        {
            return !(position1 == position2);
        }
    }
}