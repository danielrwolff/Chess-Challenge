using ChessChallenge.API;
using System;

public class EvilBotV10 : IChessBot
{
    ulong[,] mg_pesto_values = {
        {
            9187201950435737471,
            7889872735987012212,
            8249888057508007801,
            8178110886297371762,
            8684490983700006771,
            8971887951866465141,
            12736916069520674937,
            9187201950435737471,
        },
        {
            5365021282678175091,
            8098731609819740277,
            8321110065793174391,
            8683370564221962619,
            8829175787637803146,
            7466283872183886741,
            6515199656436073078,
            3121678878726059849,
        },
        {
            7961652157893667700,
            9333296259492581247,
            9189180002037172356,
            8972724671372166273,
            9043659715595305598,
            8615830923643490686,
            8252695102340499559,
            8106855828019643003,
        },
        {
            8464655719713631346,
            7599672065180073051,
            7526209278135204974,
            7886499481757909619,
            8320825326692236149,
            8973576814208589191,
            10128486292210617493,
            10346052129634094740,
        },
        {
            9112605605389037414,
            7889044859766865279,
            8683073665731954305,
            8823249286498975869,
            8174446153761783423,
            8680268833331320475,
            8316878023779126682,
            8178411053325325461,
        },
        {
            8615814228884949894,
            9188041839510782851,
            8680816273551030129,
            7385465145061371493,
            8535862196875458669,
            8830292744950942324,
            10195715779626626160,
            6812406333190340741,
        },
    };
    
    ulong[,] eg_pesto_values = {
        {
            9187201950435737471,
            9620677834192158843,
            9332158264906645115,
            9620671198299652222,
            10343507760406169479,
            12588028940747647145,
            15624621818218926556,
            9187201950435737471,
        },
        {
            8099006461787924063,
            7671172215411667817,
            8321946802664076660,
            8537848026931626358,
            8538977221128520566,
            8319701586934396266,
            8249313030004962149,
            7092175941798027085,
        },
        {
            8321090239988202614,
            8680261115071592305,
            8754297855876823927,
            8971317279073795450,
            9044219276634128512,
            9258133640255274881,
            8898411897061670264,
            8679695953109350003,
        },
        {
            8827196550388679029,
            8970184747602573693,
            9043083459462527863,
            9259826901013003129,
            9331605210574847616,
            9404222464637893757,
            9549185388996756352,
            9620964815317336961,
        },
        {
            7958270012948444522,
            8391174189128838511,
            8606808257992295553,
            8542634248410010250,
            9262369058317376401,
            8467474936499308675,
            8541516032434212479,
            8830022355671090313,
        },
        {
            7236849716174549865,
            8176709008955112566,
            8466068600795923066,
            8538131701015675769,
            8902081049221106816,
            9549753845300958597,
            8757115917098453636,
            6515994487092052342,
        },
    };

    ulong[] mg_piece_value = { 0, 82, 337, 365, 477, 1025, 10000 };
    ulong[] eg_piece_value = { 0, 94, 281, 297, 512, 936, 10000 };
    int[] phase_incs = { 0, 0, 1, 1, 2, 4, 0 };

    Move choice;
    int timeout;

#if DEBUG
    int nodes, choiceScore;
#endif
    Board board;
    Timer timer;

    (Move move, ulong key, int depth, int flag, int score)[] tt = {};
    Move[,] killers;

    int MAX = 10000000;

    public EvilBotV10() {
        Array.Resize(ref tt, 0x7FFFFF + 1);
    }

    public Move Think(Board board, Timer timer)
    {
        this.board = board;
        this.timer = timer;
        killers = new Move[60,2];

        choice = Move.NullMove;
        timeout = timer.MillisecondsRemaining / 30;

#if DEBUG
        choiceScore = -MAX;
#endif

        int depth = 1;
        while (timer.MillisecondsElapsedThisTurn < timeout && depth <= 30) {

#if DEBUG
            nodes = 0;
#endif

            int score = Search(depth++, 0, -MAX, MAX);
            if (score > MAX/2) break;

#if DEBUG
            Debug(0, 
                "MBOT depth=" + (depth-1) +
                "; nodes=" + nodes +
                "; score=" + choiceScore +
                "; time=" + timer.MillisecondsElapsedThisTurn +
                "; " + choice
            );
#endif
        }

#if DEBUG
        Debug(0, "MBOT committing " + choice + " with a score of " + choiceScore);
#endif

        // Debug this issue
        if (choice.IsNull) choice = board.GetLegalMoves()[0];
        return choice;
    }

    int Search(int depth, int ply, int alpha, int beta) {
#if DEBUG
        nodes++;
#endif

        bool queisce = depth <= 0, isRoot = ply == 0;
        ulong key = board.ZobristKey;
        int oAlpha, result = -MAX;

        if (!isRoot && board.IsRepeatedPosition()) return 0;

        var ttEntry = tt[key % 0x7FFFFF];
        if (!isRoot && ttEntry.key == key && ttEntry.depth >= depth && (
                ttEntry.flag == 0 
                || (ttEntry.flag == 1 && ttEntry.score <= alpha) 
                || (ttEntry.flag == 2 && ttEntry.score >= beta)
            )) return ttEntry.score;

        Move bestMove = ttEntry.move;

        if (queisce) {
            result = Score();
            if (result >= beta) return result;
            alpha = Math.Max(alpha, result);
        }

        Move[] moves = board.GetLegalMoves(queisce && !board.IsInCheck());
        if (!queisce && moves.Length == 0) return board.IsInCheck() ? -MAX + ply+1 : 0;

        int[] ordering = Array.ConvertAll(moves, move =>
            (move == bestMove ? MAX : 0) +
            (move.IsCapture 
                ? 1000 * (int)move.CapturePieceType - (int)move.MovePieceType 
                : (killers[ply, 0] == move || killers[ply, 1] == move ? 900 : 0))
        );

        oAlpha = alpha;

        for (int index = 0, pick; index < moves.Length; index++) {
            if (timer.MillisecondsElapsedThisTurn > timeout) {
                return MAX;
            }

            pick = index;
            for (int j = index; j < moves.Length; j++) {
                if (ordering[j] > ordering[pick]) pick = j;
            }
            (ordering[index], ordering[pick], moves[index], moves[pick]) = (ordering[pick], ordering[index], moves[pick], moves[index]);
            Move move = moves[index];

            board.MakeMove(move);
            int score;
            if (index == 0) {
                score = -Search(depth - 1, ply + 1, -beta, -alpha);
            }
            else {
                score = -Search(depth - 1, ply + 1, -alpha - 1, -alpha);
                if (alpha < score && score < beta) 
                    score = -Search(depth - 1, ply + 1, -beta, -score);
            }
            board.UndoMove(move);

            if (score > result) {
                result = score;
                bestMove = move;
                if (isRoot) {
                    choice = move;
#if DEBUG
                    choiceScore = result;
                    //Debug(0, "A=" + alpha + " B=" + beta + "; found score = " + score + "; updating current: " + choice.ToString());
#endif
                }

                if (score > alpha) {
                    alpha = score;
                    if (!move.IsCapture) {
                        // History heuristic
                    }

                }

                if (score >= beta) {
                    if (!move.IsCapture && killers[ply, 0] != move) {
                        killers[ply, 1] = killers[ply, 0];
                        killers[ply, 0] = move;
                    }
                    break;
                }
            }
        }

        if (!bestMove.IsNull) {
            tt[key % 0x7FFFFF] = (
                bestMove,
                key,
                depth,
                result >= beta ? 2 : result > oAlpha ? 0 : 1,
                result
            );
        }

        return result;
    }    

    int Score() {
        PieceList[] lists = board.GetAllPieceLists();
        int mg = 0, eg = 0, phase = 0;

        foreach (PieceList list in lists) {
            foreach (Piece piece in list) {
                int type_index = (int)piece.PieceType,
                    turn = piece.IsWhite == board.IsWhiteToMove ? 1 : -1,
                    i = type_index - 1,
                    j = Math.Abs(piece.Square.Rank - (piece.IsWhite ? 0 : 7)),
                    file = (7-piece.Square.File) * 8;
                
                ulong mgRank = mg_pesto_values[i, j],
                      egRank = eg_pesto_values[i, j];
                
                mg += (int)((((mgRank >> file) & 255) - 127) * 2 + mg_piece_value[type_index]) * turn;
                eg += (int)((((egRank >> file) & 255) - 127) * 2 + eg_piece_value[type_index]) * turn;
                phase += phase_incs[type_index];
            }
        }
        phase = Math.Min(phase, 24);

        return (mg * phase + eg * (24-phase)) / 24;
    }

#if DEBUG
    void Debug(int depth, string text) {
        System.Console.WriteLine(String.Concat(new string('\t', depth), text));
    }
#endif



}