using ChessChallenge.API;
using System;
using System.ComponentModel;
using System.IO;
using System.Net.Http;

public class MyBot : IChessBot
{
    // Piece values: Pawn, Knight, Bishop, Rook, Queen, King
    private static readonly int[] pieceValues = { 0, 1, 3, 3, 5, 9, 0 };
    const int maxDepth = 3;
    Move moveToPlay = Move.NullMove;

    public Move Think(Board board, Timer timer)
    {
        Search(board, maxDepth, int.MinValue + 1, int.MaxValue - 1);
        return moveToPlay;
    }

    // TODO: Combine with Quiesce search into 1 function.
    // Inspired from: https://www.chessprogramming.org/Alpha-Beta
    private int Search(Board board, int depth, int alpha, int beta)
    {
        if (depth == 0 || board.IsInCheckmate() || board.IsDraw())
            return Quiesce(board, alpha, beta);

        Span<Move> moves = stackalloc Move[256];
        board.GetLegalMovesNonAlloc(ref moves);
        foreach (Move move in moves)
        {
            // Make a move and recurse.
            board.MakeMove(move);
            int score = -Search(board, depth - 1, -beta, -alpha);
            board.UndoMove(move);

            // Check if this is a bad branch or if we found a new best move.
            if (score >= beta)
                return beta;
            if (score > alpha)
            {
                alpha = score;
                if (depth == maxDepth)
                    moveToPlay = move;
            }
        }
        return alpha;
    }

    // Inspired from: https://www.chessprogramming.org/Quiescence_Search
    private int Quiesce(Board board, int alpha, int beta)
    {
        int staticEval = Evaluate(board);

        // Stand Pat
        int bestValue = staticEval;
        if (bestValue >= beta)
            return bestValue;
        if (bestValue > alpha)
            alpha = bestValue;

        Span<Move> moves = stackalloc Move[256];
        board.GetLegalMovesNonAlloc(ref moves, true);
        foreach (Move move in moves)
        {
            board.MakeMove(move);
            int score = -Quiesce(board, -beta, -alpha);
            board.UndoMove(move);

            if (score >= beta)
                return score;
            if (score > bestValue)
                bestValue = score;
            if (score > alpha)
                alpha = score;
        }

        return bestValue;
    }

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
        eval += 10 * EvaluateMaterial(board);
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
