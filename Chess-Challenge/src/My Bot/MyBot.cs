using ChessChallenge.API;
using System;

public class MyBot : IChessBot
{
    // Piece values: Pawn, Knight, Bishop, Rook, Queen, King
    private static readonly int[] pieceValues = { 0, 1, 3, 3, 5, 9, 0 };

    public Move Think(Board board, Timer timer)
    {
        // TODO: Check some examples to see if it is better to make this stackallow shared between calls.
        Span<Move> moves = stackalloc Move[256];
        board.GetLegalMovesNonAlloc(ref moves);

        Move bestMove = moves[0];
        int bestEval = int.MinValue;

        foreach (Move move in moves)
        {
            board.MakeMove(move);
            int eval = -AlphaBeta(board, 4, int.MinValue + 1, int.MaxValue - 1);
            board.UndoMove(move);

            if (eval > bestEval)
            {
                bestEval = eval;
                bestMove = move;
            }
        }

        return bestMove;
    }

    // TODO: Add some stuff like transposition tables, iterative deepening, and quiescence search.
    private int AlphaBeta(Board board, int depth, int alpha, int beta)
    {
        if (depth == 0 || board.IsInCheckmate() || board.IsDraw())
            return Evaluate(board);

        Span<Move> moves = stackalloc Move[256];
        board.GetLegalMovesNonAlloc(ref moves);

        int bestEval = int.MinValue;

        if (moves.Length == 0)
            return Evaluate(board);

        foreach (Move move in moves)
        {
            board.MakeMove(move);
            int eval = -AlphaBeta(board, depth - 1, -beta, -alpha);
            board.UndoMove(move);

            if (eval > bestEval)
                bestEval = eval;
            if (bestEval > alpha)
                alpha = bestEval;
            if (alpha >= beta)
                break; // Beta cutoff
        }

        return bestEval == int.MinValue ? Evaluate(board) : bestEval;
    }

    // TODO: Would be cool to improve this evaluation function.
    private int Evaluate(Board board)
    {
        int eval = EvaluateForWhite(board);
        return eval * (board.IsWhiteToMove ? 1 : -1);
    }

    // Always evaluates from White's perspective
    private int EvaluateForWhite(Board board)
    {
        if (board.IsInCheckmate())
            return board.IsWhiteToMove ? -10000 : 10000;
        if (board.IsDraw())
            return 0;

        int eval = 0;
        eval += 2 * EvaluateMaterial(board);
        eval += EvaluateKnightCentralization(board);
        eval += EvaluatePawnAdvancement(board);

        return eval;
    }

    private int EvaluateMaterial(Board board)
    {
        int eval = 0;
        for (int i = 1; i <= 5; i++)
            eval += pieceValues[i] * board.GetPieceList((PieceType)i, true).Count;
        for (int i = 1; i <= 5; i++)
            eval -= pieceValues[i] * board.GetPieceList((PieceType)i, false).Count;
        return eval;
    }

    private int EvaluateKnightCentralization(Board board)
    {
        int eval = 0;
        foreach (var knight in board.GetPieceList(PieceType.Knight, true))
        {
            if (knight.Square.File >= 2 && knight.Square.File <= 5 &&
                knight.Square.Rank >= 2 && knight.Square.Rank <= 5)
                eval += 1;
        }
        foreach (var knight in board.GetPieceList(PieceType.Knight, false))
        {
            if (knight.Square.File >= 2 && knight.Square.File <= 5 &&
                knight.Square.Rank >= 2 && knight.Square.Rank <= 5)
                eval -= 1;
        }
        return eval;
    }

    private int EvaluatePawnAdvancement(Board board)
    {
        int eval = 0;
        foreach (var pawn in board.GetPieceList(PieceType.Pawn, true))
            eval += pawn.Square.Rank;
        foreach (var pawn in board.GetPieceList(PieceType.Pawn, false))
            eval -= (7 - pawn.Square.Rank);
        return eval;
    }
}
