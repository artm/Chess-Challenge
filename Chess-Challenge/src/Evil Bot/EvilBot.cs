using ChessChallenge.API;
using System;
using System.Linq;

namespace ChessChallenge.Example
{
    // A simple bot that can spot mate in one, and always captures the most valuable piece it can.
    // Plays randomly otherwise.
    public class OriginalEvilBot : IChessBot
    {
        // Piece values: null, pawn, knight, bishop, rook, queen, king
        int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };

        public Move Think(Board board, Timer timer)
        {
            Move[] allMoves = board.GetLegalMoves();

            // Pick a random move to play if nothing better is found
            Random rng = new();
            Move moveToPlay = allMoves[rng.Next(allMoves.Length)];
            int highestValueCapture = 0;

            foreach (Move move in allMoves)
            {
                // Always play checkmate in one
                if (MoveIsCheckmate(board, move))
                {
                    moveToPlay = move;
                    break;
                }

                // Find highest value capture
                Piece capturedPiece = board.GetPiece(move.TargetSquare);
                int capturedPieceValue = pieceValues[(int)capturedPiece.PieceType];

                if (capturedPieceValue > highestValueCapture)
                {
                    moveToPlay = move;
                    highestValueCapture = capturedPieceValue;
                }
            }

            return moveToPlay;
        }

        // Test if this move gives checkmate
        bool MoveIsCheckmate(Board board, Move move)
        {
            board.MakeMove(move);
            bool isMate = board.IsInCheckmate();
            board.UndoMove(move);
            return isMate;
        }
    }

    public class MyBot_0_3 : IChessBot
    {
        enum BoundType { Lower = -1, Exact, Upper }

        struct Position {
            public ulong Key;
            public Move Move;
            public BoundType Bound;
            public int Score;
            public int Depth;

            public bool IsCutoff(Board board, int depth, int alpha, int beta) {
                return
                    Key == board.ZobristKey && Depth >= depth && (
                        Bound == BoundType.Exact
                        || Bound == BoundType.Lower && Score >= beta
                        || Bound == BoundType.Upper && Score <= alpha
                    );
            }
        }

        class OutOfTime : System.Exception {}

        Board board;
        Timer timer;
        Move bestRootMove;
        const int WinScore = 100000, Inf = 1000000, TTSize = 2^20;
        int[] PieceValue = { 0, 100, 300, 300, 400, 500, 10000 };
        Position[] Transpositions = new Position[TTSize];

        public Move Think(Board board, Timer timer)
        {
            this.board = board;
            this.timer = timer;
            for(int depth=1; MayThink(); depth++) {
                try {
                    Search(depth);
                } catch (OutOfTime) {
                    // it's ok, we'll have the best move from the previous iteration
                }
            }
            return bestRootMove;
        }

        int Search(int depth, int dFromRoot = 0, int alpha = -Inf, int beta = Inf)
        {
            bool quiescence = depth <= 0;
            if (board.IsInCheckmate())
                return dFromRoot - WinScore;
            else if (board.IsInStalemate())
                return 0;
            else if (quiescence) {
                var score = Evaluate();
                if (score >= beta)
                    return score;
                alpha = score;
            }

            ref Position tr = ref Transpositions[ board.ZobristKey % TTSize ];
            if (dFromRoot > 0 && tr.IsCutoff(board, depth, alpha, beta)) {
                return tr.Score;
            }

            var moves = board.GetLegalMoves(quiescence);
            int[] moveScores = ScoreMoves(moves, tr.Move);
            Move bestMove = Move.NullMove;
            for(int i=0; i<moves.Length; i++) {
                if (!MayThink()) throw new OutOfTime();
                var move = FindNextMove(moves, moveScores, i);
                board.MakeMove(move);
                int score = - Search(depth - 1, dFromRoot + 1, - beta, - alpha);
                board.UndoMove(move);
                if (score > alpha) {
                    alpha = score;
                    bestMove = move;
                    if (alpha >= beta) break;
                }
            }
            tr.Key = board.ZobristKey;
            tr.Move = bestMove;
            tr.Bound =
                alpha >= beta ? BoundType.Lower :
                bestMove.IsNull ? BoundType.Upper :
                BoundType.Exact;
            tr.Score = alpha;
            tr.Depth = depth;
            if (dFromRoot == 0) bestRootMove = bestMove;
            return alpha;
        }

        int Evaluate()
        {
            int score = 0;
            for(PieceType pt=PieceType.Pawn; pt<PieceType.King; pt++) {
                score += PieceValue[(int)pt] * (
                    board.GetPieceList(pt, board.IsWhiteToMove).Count -
                    board.GetPieceList(pt, !board.IsWhiteToMove).Count);
            }
            return score;
        }

        int[] ScoreMoves(Move[] moves, Move bestMove)
        {
            var scores = new int[moves.Length];
            for(int i=0; i<moves.Length; i++) {
                if (moves[i] == bestMove)
                    scores[i] = Inf;
                else if (moves[i].IsCapture)
                    scores[i] = 100 * (int)moves[i].CapturePieceType - (int)moves[i].MovePieceType;
            }
            return scores;
        }

        Move FindNextMove(Move[] moves, int[] scores, int i)
        {
            var (_, iMax) = scores.Select((score, i) => (score, i)).Skip(i).Max();
            (scores[i], scores[iMax]) = (scores[iMax], scores[i]);
            (moves[i], moves[iMax]) = (moves[iMax], moves[i]);
            return moves[i];
        }

        bool MayThink()
        {
            return timer.MillisecondsElapsedThisTurn < timer.MillisecondsRemaining / 60;
        }

    }

    public class EvilBot : MyBot_0_3 {}
}
