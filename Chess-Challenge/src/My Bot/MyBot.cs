using ChessChallenge.API;
using System;
using System.Collections.Generic;

public class MyBot : IChessBot {
    public bool isPlayingWhite;
    // Transposition table
    Dictionary<ulong, int> transpositionTable = new Dictionary<ulong, int>();
    Dictionary<ulong, int> quiesceTable = new Dictionary<ulong, int>();
    ulong[] lastReachedPositions = new ulong[20];

    public Move Think(Board board, Timer timer) {
        isPlayingWhite = board.IsWhiteToMove;
        Move move = PickMove(board, timer.MillisecondsRemaining > 25000 ? 8 : 6);
        // Add to lastReachedPositions
        board.MakeMove(move);
        for(int i = 0; i < lastReachedPositions.Length - 1; i++) {
            lastReachedPositions[i] = lastReachedPositions[i + 1];
        }
        lastReachedPositions[lastReachedPositions.Length - 1] = board.ZobristKey;
        return move;
    }

    public Move PickMove(Board board, int depth) {
        Move bestMove = new Move();
        int alpha = -2147483647;

        Move[] moves = board.GetLegalMoves();
        foreach(Move move in moves) {
            board.MakeMove(move);
            int value = -Alphabeta(
                -999999998,
                -alpha,
                depth - DepthExtension(board),
                board
            );
            // If in lastReachedPositions, halve the value
            for(int i = 0; i < lastReachedPositions.Length; i++) {
                if (lastReachedPositions[i] == board.ZobristKey) {
                    value /= 2;
                    break;
                }
            }
            board.UndoMove(move);
            if(value > alpha) {
                alpha = value;
                bestMove = move;
            }
        }

        // Console.WriteLine(alpha);

        return bestMove;
    }

    public int Alphabeta(int alpha, int beta, int depthleft, Board board) {
        if(depthleft <= 0) {
            return Quiesce(alpha, beta, 4, board);
        }

        Move[] moves = board.GetLegalMoves();
        foreach(Move move in moves) {
            board.MakeMove(move);
            int score;
            if(!transpositionTable.TryGetValue(board.ZobristKey, out score)) {
                score = -Alphabeta(-beta, -alpha, depthleft - DepthExtension(board), board);
                transpositionTable.TryAdd(board.ZobristKey, score);
            }
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

    public int Quiesce(int alpha, int beta, int depthleft, Board board) {
        int stand_pat = Evaluate(board);
        if(depthleft <= 0) {
            return stand_pat;
        }

        if(stand_pat >= beta) {
            return beta;
        }
        if(alpha < stand_pat) {
            alpha = stand_pat;
        }

        Move[] moves = board.GetLegalMoves();
        foreach(Move move in moves) {
            if(move.IsCapture || move.IsPromotion) {
                board.MakeMove(move);
                int score;
                if(!quiesceTable.TryGetValue(board.ZobristKey, out score)) {
                    score = -Quiesce(-beta, -alpha, depthleft - DepthExtension(board), board);
                    quiesceTable.TryAdd(board.ZobristKey, score);
                }
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

    public int DepthExtension(Board board) {
        if(board.IsInCheck()) {
            return 1;
        }
        return 2;
    }

    public int Evaluate(Board board) {
        // 1 if the optimizing side is to move, -1 if the minimizing side is to move
        int sideMultiplier = isPlayingWhite ? 1 : -1;
        // Mobility calculations
        int value = 15 * board.GetLegalMoves().Length + 30 * board.GetLegalMoves(true).Length * sideMultiplier;
        if(board.TrySkipTurn()) {
            value -= 15 * board.GetLegalMoves().Length + 30 * board.GetLegalMoves(true).Length * sideMultiplier;
            board.UndoSkipTurn();
        }

        // White pawns, White knights, White bishops, White rooks, White queens, White king, Black pawns, Black knights, Black bishops, Black rooks, Black queens, Black king
        PieceList[] pieces = board.GetAllPieceLists();
        int[] pieceValues = new int[6] { 100, 200, 230, 500, 900, 5000 };
        for(int i = 0; i < 12; i++) {
            value += pieces[i].Count * (i >= 6 ? -pieceValues[i - 6] : pieceValues[i]) * sideMultiplier;
        }

        if (board.IsInCheckmate()) {
            value += 100000 * sideMultiplier;
        }
        if(board.IsInCheck()) {
            value += -150 * sideMultiplier;
        }

        return value;
    }
}