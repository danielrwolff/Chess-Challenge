using ChessChallenge.API;
using System;

public class EvilBot : IChessBot
{
    ulong[,] mg_pesto_values = {
        {
            8680820740569200760,
            6879082848772002664,
            7310878700352475247,
            7239101546372038502,
            7962782537720300647,
            8321961131264477801,
            13751922592385831024,
            8680820740569200760,
        },
        {
            3200175186265795175,
            7156905312447196778,
            7453880143464139372,
            7961101358739064690,
            8179809739323639176,
            6242994757456735383,
            4853381293802814828,
            15867420127423787,
        },
        {
            6950864473996352617,
            8900101842139582585,
            8683929107354584447,
            8323076022782164859,
            8465506832093445494,
            7823482171344851574,
            7314808342346630486,
            7168390703248735602,
        },
        {
            7669200825507208549,
            6371583508879536965,
            6297840342091199584,
            6801965371226291303,
            7453595421560107625,
            8396270541708240003,
            10056435321180556183,
            10346615101179465367,
        },
        {
            8605096305131675988,
            6877700840250111609,
            7960517470416634491,
            8171061831066286710,
            7234026114581296761,
            7956872624152287905,
            7447955948626349215,
            7239691984621246360,
        },
        {
            7895514184235125122,
            8754278914331541118,
            7957412315964534116,
            6158501363217621075,
            7811897505589128798,
            8181203704628414568,
            10193732226138856547,
            5298629467107719553,
        },
    };
    
    ulong[,] eg_pesto_values = {
        {
            8680820740569200760,
            9331034564056349043,
            8898395404303103858,
            9331025716188445303,
            10342940386501821572,
            13601070610512786356,
            17939221432963166207,
            8680820740569200760,
        },
        {
            7157177952422810698,
            6514863080984897368,
            7455277639821322600,
            7742676815315237739,
            7816710223241969259,
            7451627239607724634,
            7382075398108374866,
            5646509410225506864,
        },
        {
            7453852608091681900,
            7956580123689446756,
            8031746071560090477,
            8321105671856551537,
            8538403224384666233,
            8751189480418605179,
            8247636210280723822,
            7956013853592087655,
        },
        {
            8176701251940940649,
            8319125399673073782,
            8464643556029461100,
            8825786963462419056,
            8897566372552865657,
            9042521596135634038,
            9259824697593986682,
            9331323752778071675,
        },
        {
            6945791258726066522,
            7523089920360406625,
            7810512224489078651,
            7749715957661078664,
            8829458358098829714,
            7601089477872813438,
            7820095675815333240,
            8180938856041119622,
        },
        {
            5935578765339813721,
            7237420397354513516,
            7671460334871477617,
            7743244159154683504,
            8252997549591399290,
            9188619238222894977,
            8035691140807362688,
            4782659438484093804,
        },
    };

    ulong[] mg_piece_value = { 0, 82, 337, 365, 477, 1025, 0 };
    ulong[] eg_piece_value = { 0, 94, 281, 297, 512, 936, 0 };
    int[] phase_incs = { 0, 0, 1, 1, 2, 4, 0 };

    Move choice;
    int timeout;

    Board board;
    Timer timer;

    (Move move, ulong key, int depth, int flag, int score)[] tt = {};

    int MAX = 10000000;

    public EvilBot() {
        Array.Resize(ref tt, 0x7FFFFF + 1);
    }

    public Move Think(Board board, Timer timer)
    {
        this.board = board;
        this.timer = timer;

        timeout = timer.MillisecondsRemaining / 100;
        choice = Move.NullMove;

        for (int depth = 1; depth < 30; depth++) {

            Search(depth, 0, -MAX, MAX);

            if (timer.MillisecondsElapsedThisTurn > timeout) {
                break;
            }
        }

        return choice;
    }

    int Search(int depth, int ply, int alpha, int beta) {
        if (board.IsInsufficientMaterial() || board.IsRepeatedPosition() || board.FiftyMoveCounter >= 100)
            return 0;

        bool queisce = depth <= 0;
        ulong key = board.ZobristKey;
        int oAlpha = alpha, result = -MAX;

        var ttEntry = tt[key % 0x7FFFFF];
        if (ply > 0 && ttEntry.key == key && ttEntry.depth >= depth) {
            if (ttEntry.flag == 0) return ttEntry.score;
            if (ttEntry.flag == 1) alpha = Math.Max(alpha, ttEntry.score);
            if (ttEntry.flag == 2) beta = Math.Min(beta, ttEntry.score);
            if (alpha >= beta) return ttEntry.score; 
        }

        Move bestMove = ttEntry.move;

        if (queisce) {
            result = Score();
            if (result >= beta) return result;
            alpha = Math.Max(alpha, result);
        }

        Move[] moves = board.GetLegalMoves(queisce);
        if (!queisce && moves.Length == 0) return board.IsInCheck() ? -MAX + ply : 0;

        foreach (Move move in OrderMoves(ref moves, bestMove)) {
            board.MakeMove(move);
            int score = -Search(depth - 1, ply + 1, -beta, -alpha);
            board.UndoMove(move);

            if (timer.MillisecondsElapsedThisTurn > timeout) {
                return -MAX;
            }

            if (score > result) {
                result = score;
                bestMove = move;
                if (ply == 0) {
                    choice = move;
                }
            }

            alpha = Math.Max(alpha, score);
            if (alpha >= beta) break;
        }

        if (bestMove != Move.NullMove) {
            tt[key % 0x7FFFFF] = (
                bestMove,
                key,
                depth,
                result <= oAlpha ? 2 : result >= beta ? 1 : 0,
                result
            );
        }

        return result;
    }

    ref Move[] OrderMoves(ref Move[] moves, Move priority) {
        Array.Sort(
            Array.ConvertAll(moves, move => {
                if (move == priority) return -MAX;
                int after = GetPieceValue(move.MovePieceType, move.TargetSquare, board.IsWhiteToMove);
                int before = GetPieceValue(move.MovePieceType, move.StartSquare, board.IsWhiteToMove);
                return -(
                    after - before
                    + (move.IsCapture ? 100 * GetPieceValue(move.CapturePieceType, move.TargetSquare, !board.IsWhiteToMove) - before : 0)
                    + (move.IsPromotion ? 5 * GetPieceValue(move.PromotionPieceType, move.TargetSquare, board.IsWhiteToMove) : 0)
                );
            }), 
            moves
        );
        return ref moves;
    }

    int Score() {
        PieceList[] lists = board.GetAllPieceLists();
        int score = 0, phase = 0;

        foreach (PieceList list in lists) {
            foreach (Piece piece in list) {
                phase += phase_incs[(int)piece.PieceType];
            }
        }
        phase = Math.Min(phase, 24);

        foreach (PieceList list in lists) {
            foreach (Piece piece in list) {
                score += GetPieceValue(piece.PieceType, piece.Square, piece.IsWhite, (ulong)phase) * (piece.IsWhite == board.IsWhiteToMove ? 1 : -1);
            }
        }

        return score;
    }

    int GetPieceValue(PieceType type, Square square, Boolean isWhite, ulong phase = 12) {
        int type_index = (int)type;
        ulong mgRank = mg_pesto_values[type_index - 1, Math.Abs(square.Rank - (isWhite ? 0 : 7))];
        ulong mgScore = ((mgRank >> (square.File * 8)) & 255) - 127 + mg_piece_value[type_index];
        ulong egRank = eg_pesto_values[type_index - 1, Math.Abs(square.Rank - (isWhite ? 0 : 7))];
        ulong egScore = ((egRank >> (square.File * 8)) & 255) - 127 + eg_piece_value[type_index];
        return (int)(mgScore * phase + egScore * (24 - phase)) / 24;
    }
}