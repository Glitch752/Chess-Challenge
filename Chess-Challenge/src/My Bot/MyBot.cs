using ChessChallenge.API;
using System;

public class MyBot : IChessBot {
    public Move Think(Board board, Timer timer) {
        return PickMove(board, 3);
    }

    public Move PickMove(Board board, int depth) {
        Move bestMove = new Move();
        int bestValue = -999999;
        int alpha = -999999;

        Move[] moves = board.GetLegalMoves();
        foreach(Move move in moves) {
            board.MakeMove(move);
            int value = -Alphabeta(-999999, -alpha, depth - 1, board);
            board.UndoMove(move);
            if(value > bestValue) {
                bestValue = value;
                bestMove = move;
            }
            if(value > alpha) {
                alpha = value;
            }
        }

        Console.WriteLine(alpha);

        return bestMove;
    }

    public int Alphabeta(int alpha, int beta, int depthleft, Board board) {
        if(depthleft == 0) {
            return Quiesce(alpha, beta, board);
        }

        Move[] moves = board.GetLegalMoves();
        foreach(Move move in moves) {
            board.MakeMove(move);
            int score = -Alphabeta(-beta, -alpha, depthleft - 1, board);
            board.UndoMove(move);
            if(score >= beta) {
                // Alpha-beta pruning; we can stop searching because we know this is a bad move
                return beta;
            }
            if(score > alpha) {
                alpha = score;
            }
        }
        return alpha;
    }

    public int Quiesce(int alpha, int beta, Board board) {
        int stand_pat = Evaluate(board);
        if(stand_pat >= beta) {
            return beta;
        }
        if(alpha < stand_pat) {
            alpha = stand_pat;
        }

        Move[] moves = board.GetLegalMoves();
        foreach(Move move in moves) {
            if(move.IsCapture) {
                board.MakeMove(move);
                int score = -Quiesce(-beta, -alpha, board);
                board.UndoMove(move);

                if(score >= beta) {
                    return beta;
                }
                if(score > alpha) {
                    alpha = score;
                }
            }
        }
        return alpha;
    }

    public int Evaluate(Board board) {
        if(board.IsInCheckmate()) {
            return board.IsWhiteToMove ? 10000 : -10000;
        }

        // White pawns, White knights, White bishops, White rooks, White queens, White king, Black pawns, Black knights, Black bishops, Black rooks, Black queens, Black king
        PieceList[] pieces = board.GetAllPieceLists();
        int[] pieceValues = new int[6] { 1, 2, 2, 5, 9, 0 };
        int materialValueSum = 0;
        for(int i = 0; i < 12; i++) {
            materialValueSum += pieces[i].Count * (i >= 6 ? -pieceValues[i-6] : pieceValues[i]);
        }

        return materialValueSum;
    }
}