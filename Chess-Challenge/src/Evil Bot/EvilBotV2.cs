using ChessChallenge.API;
using System;

namespace ChessChallenge.Example
{
    // A simple bot that can spot mate in one, and always captures the most valuable piece it can.
    // Plays randomly otherwise.
    public class EvilBotV2 : IChessBot
    {
        // EvilBot V2 (alpha/beta pruning)
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

        Move move;
        Board board;
        Timer timer;

        int MAX = 10000000;

        public Move Think(Board board, Timer timer)
        {
            this.board = board;
            this.timer = timer;

            for (int depth = 1; depth < 100; depth++) {
                
                move = Move.NullMove;

                int score = Search(depth, 0, -MAX, MAX);
                //Debug(0, "Depth: " + depth.ToString());
                //Debug(0, "Score: " + score.ToString());
                //Debug(0, "Move: " + move.ToString());
                //Debug(0, "Time: " + timer.MillisecondsElapsedThisTurn.ToString());

                if (timer.MillisecondsElapsedThisTurn > 150) {
                    break;
                }
            }

            //Debug(0, "MBOT Time took: " + timer.MillisecondsElapsedThisTurn.ToString());
            //Debug(0, "MBOT Score: " + score.ToString());
            //Debug(0, "MBOT move: " + move.ToString());

            //System.Threading.Thread.Sleep(1000);

            return move;
        }

        int Search(int depth, int ply, int alpha, int beta) {
            if (depth <= 0) return Score();
            
            Move[] moves = board.GetLegalMoves();
            if (moves.Length == 0 && board.IsInCheck()) return -MAX + ply;

            //int best = -MAX;
            foreach (Move next in moves) {
                board.MakeMove(next);
                int score = -Search(depth - 1, ply + 1, -beta, -alpha);
                board.UndoMove(next);

                //Debug(ply, "Move " + next.ToString());
                //Debug(ply, "score: " + score);
                //Debug(ply, "alpha: " + alpha);
                //Debug(ply, "beta : " + beta);

                if (score > alpha) {
                    if (ply == 0) move = next;
                    if (score >= beta) return beta;
                    alpha = score;
                }
            }

            //return best;
            return alpha;
        }

        int Quiesce(int alpha, int beta) {
            int pat = Score();
            if (pat >= beta) return beta;
            if (alpha < pat) alpha = pat;

            foreach(Move next in board.GetLegalMoves(true)) {
                board.MakeMove(next);
                int score = -Quiesce(-beta, -alpha);
                board.UndoMove(next);

                if (score >= beta) return beta;
                alpha = Math.Max(alpha, score);
            }

            return alpha;
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
}