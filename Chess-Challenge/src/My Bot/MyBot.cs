﻿using ChessChallenge.API;
using System;

public class MyBot : IChessBot
{
    ulong[,] tables = {
        {
            4629771061636907072,
            4992884690475698757,
            4988640661826059077,
            4629771147871797312,
            4991477509620647237,
            5353183871369497162,
            8246779703540740722,
            4629771061636907072,
        },
        {
            1015599245968939022,
            1741837822144293912,
            2469461675175724322,
            2468059819409358882,
            2469467194292913442,
            2468054300292169762,
            1741837800585571352,
            1015599245968939022,
        },
        {
            3185793392876860972,
            3910602496141182262,
            3912020909259115062,
            3909206159492005942,
            3910608036817093942,
            3909200661933539382,
            3909195121257627702,
            3185793392876860972,
        },
        {
            4629771083195629632,
            4269483091447267387,
            4269483091447267387,
            4269483091447267387,
            4269483091447267387,
            4269483091447267387,
            4992884819828034117,
            4629771061636907072,
        },
        {
            3185793414435583532,
            3909200618815766582,
            3910608015258370102,
            4629776580754096187,
            4269488610564456507,
            3909200640374816822,
            3909195121257627702,
            3185793414435583532,
        },
        {
            6079378186813726292,
            6076552441929684052,
            3903543545254652982,
            3180141773756441132,
            2456740045375674402,
            2456740045375674402,
            2456740045375674402,
            2456740045375674402,
        },
    };
    
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };

    Move choiceMove, currentMove;
    int choiceScore, currentScore;
    Board board;
    Timer timer;

    int MAX = 10000000;

    /* TODO
        - Transposition tables
        - Implement Queisce search into search func to save tokens.
        - Take advantage of 5 second init for constructor (populate tranposition tables)?
    */

    public Move Think(Board board, Timer timer)
    {
        this.board = board;
        this.timer = timer;

        choiceMove = Move.NullMove;

        for (int depth = 1; depth < 99; depth++) {

            currentMove = Move.NullMove;
            Search(depth, 0, -MAX, MAX);

            //Debug(0, "============ Depth: " + depth.ToString());
            //Debug(0, "Score: " + score.ToString());
            //Debug(0, "Move: " + move.ToString());
            //Debug(0, "Time: " + timer.MillisecondsElapsedThisTurn.ToString());

            if (timer.MillisecondsElapsedThisTurn > 500) {
                //Debug(0, "Max depth: " + depth.ToString());
                break;
            }
            if (currentMove != Move.NullMove) {
                choiceMove = currentMove;
                choiceScore = currentScore;
            }
        }

        Debug(0, "POSITION: " + board.GetFenString());
        Debug(0, "MBOT player: " + (board.IsWhiteToMove ? "White" : "Black"));
        Debug(0, "MBOT time took: " + timer.MillisecondsElapsedThisTurn.ToString());
        Debug(0, "MBOT move: " + choiceMove.ToString());
        Debug(0, "MBOT score: " + choiceScore.ToString());

        //System.Threading.Thread.Sleep(1000);

        //Debug(0, "PLAYING: " + choice.ToString());
        return choiceMove;
    }

    int Search(int depth, int ply, int alpha, int beta) {
        if (depth <= 0) return Quiesce(alpha, beta);
        if (board.IsDraw()) return 0;

        Move[] moves = board.GetLegalMoves();
        if (moves.Length == 0 && board.IsInCheck()) return -MAX + ply;

        int best = -MAX + 1;
        foreach (Move next in OrderMoves(ref moves)) {
            board.MakeMove(next);
            int extension = (ply < 16 && board.IsInCheck()) ? 1 : 0;
            int score = -Search(depth - 1 + extension, ply + 1, -beta, -alpha);
            board.UndoMove(next);

            if (timer.MillisecondsElapsedThisTurn > 500) {
                return -MAX;
            }

            /*
            if (ply == 0) {
                Debug(0, "--");
                Debug(ply, next.ToString());
                Debug(ply, "score: " + score);
                Debug(ply, "alpha: " + alpha);
                Debug(ply, "beta : " + beta);
                Debug(ply, "Current : " + current.ToString());
                Debug(ply, "Choice : " + choice.ToString());
            }  
            */
            

            if (score > best) {
                best = score;
                alpha = Math.Max(alpha, best);
                if (ply == 0) {
                    currentMove = next;
                    currentScore = best;
                    //Debug(0, "A=" + alpha + " B=" + beta + "; found score = " + best + "; updating current: " + current.ToString());
                }
                if (alpha >= beta) break;
            }
        }

        return best;
    }

    int Quiesce(int alpha, int beta) {
        int pat = Score();
        if (pat >= beta) return beta;
        if (alpha < pat) alpha = pat;

        int best = pat;
        foreach(Move next in board.GetLegalMoves(true)) {
            board.MakeMove(next);
            int score = -Quiesce(-beta, -alpha);
            board.UndoMove(next);

            if (score > best) {
                best = score;
                alpha = Math.Max(alpha, best);
                if (alpha >= beta) break;
            }
        }

        return best;
    }

    ref Move[] OrderMoves(ref Move[] moves) {
        Array.Sort(
            Array.ConvertAll(moves, move => {
                int value = 10 * pieceValues[(int)move.CapturePieceType] - pieceValues[(int)move.MovePieceType];
                if (move.IsPromotion) value += pieceValues[(int)move.PromotionPieceType];
                return -value; // Want the best options to come first, so they should have the smallest values.
            }), 
            moves
        );
        return ref moves;
    }

    int Score() {
        PieceList[] lists = board.GetAllPieceLists();
        
        int score = 0;
        for (int i = 0; i < 12; i++) {
            foreach (Piece piece in lists[i]) {
                ulong rank = tables[i % 6, Math.Abs(piece.Square.Rank - (piece.IsWhite ? 0 : 7))];
                int turn = (piece.IsWhite == board.IsWhiteToMove ? 1 : -1);
                score += (int)(((rank >> (piece.Square.File * 8)) & 127) - 64) * turn;
                score += pieceValues[(int)piece.PieceType] * turn;
            }
        }

        return score;
    }

    void Debug(int depth, string text) {
        System.Console.WriteLine(String.Concat(new string('\t', depth), text));
    }

}