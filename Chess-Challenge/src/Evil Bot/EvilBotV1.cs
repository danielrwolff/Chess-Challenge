using ChessChallenge.API;
using System;

namespace ChessChallenge.Example
{
    // A simple bot that can spot mate in one, and always captures the most valuable piece it can.
    // Plays randomly otherwise.
    public class EvilBotV1 : IChessBot
    {
        // MyBot V1 (negamax, no pruning)
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

        int startingDepth = 3;

        public Move Think(Board board, Timer timer)
        {
            move = Move.NullMove;
            int score = Search(board, 0, 0, startingDepth);
            
            //Debug(0, "EVIL Time took: " + timer.MillisecondsElapsedThisTurn.ToString());
            //Debug(0, "EVIL Score: " + score.ToString());
            return move;
        }

        int Search(Board board, int alpha, int beta, int depth) {
            if (depth == 0) return -Score(board);

            int best = -2147483647;
            foreach (Move next in board.GetLegalMoves()) {
                board.MakeMove(next);
                int eval = -Search(board, alpha, beta, depth - 1);
                board.UndoMove(next);
                if (eval > best) {
                    best = eval;
                    if (startingDepth == depth) move = next;
                }
            }

            return best;
        }

        int Score(Board board) {
            PieceList[] lists = board.GetAllPieceLists();
            
            if (board.IsInCheckmate()) return 2147483647;
            
            int score = 0;
            for (int i = 0; i < 12; i++) {
                foreach (Piece piece in lists[i]) {
                    ulong rank = tables[i % 6, Math.Abs(piece.Square.Rank - (piece.IsWhite ? 0 : 7))];
                    int turn = (piece.IsWhite == !board.IsWhiteToMove ? 1 : -1);
                    score += (int)(((rank >> (piece.Square.File * 8)) & 127) - 64) * turn;
                    score += pieceValues[(int)piece.PieceType] * turn;
                }
            }

            return score;
        }

        void Debug(int depth, string text) {
            System.Console.WriteLine(String.Concat(new string('\t', startingDepth - depth), text));
        }
    }
}