using ChessChallenge.API;
using System;

public class EvilBotV9 : IChessBot
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

    int MAX = 10000000;

    public EvilBotV9() {
        Array.Resize(ref tt, 0x7FFFFF + 1);
    }

    public Move Think(Board board, Timer timer)
    {
        this.board = board;
        this.timer = timer;

        timeout = timer.MillisecondsRemaining / 100; //(board.PlyCount < 10 ? 1000 : 125);
        choice = Move.NullMove;

#if DEBUG
        choiceScore = -MAX;
#endif

        for (int depth = 1; depth < 30; depth++) {

#if DEBUG
            nodes = 0;
#endif

            Search(depth, 0, -MAX, MAX);

#if DEBUG
            /*
            Debug(0, 
                "MBOT depth=" + depth +
                "; nodes=" + nodes +
                "; score=" + choiceScore +
                "; time=" + timer.MillisecondsElapsedThisTurn +
                "; move=" + choice
            );
            */
#endif

            if (timer.MillisecondsElapsedThisTurn > timeout) {
                break;
            }
        }

#if DEBUG
        //Debug(0, "MBOT committing " + choice + " with a score of " + choiceScore);
        if (choice == Move.NullMove) throw new Exception("null move");
#endif

        return choice;
    }

    int Search(int depth, int ply, int alpha, int beta) {
#if DEBUG
        nodes++;
#endif

        if (board.IsInsufficientMaterial() || board.IsRepeatedPosition() || board.FiftyMoveCounter >= 100)
            return 0;

        bool queisce = depth <= 0;
        ulong key = board.ZobristKey;
        int oAlpha = alpha, result = -MAX;

        var ttEntry = tt[key % 0x7FFFFF];
        if (ply > 0 && ttEntry.key == key && ttEntry.depth >= depth) {
            if (ttEntry.flag == 0 
                || (ttEntry.flag == 1 && ttEntry.score <= alpha) 
                || (ttEntry.flag == 2 && ttEntry.score >= beta)
            ) return ttEntry.score;  
        }

        Move bestMove = ttEntry.move;

        if (queisce) {
            result = Score();
            if (result >= beta) return result;
            alpha = Math.Max(alpha, result);
        }

        Move[] moves = board.GetLegalMoves(queisce);
        if (!queisce && moves.Length == 0) return board.IsInCheck() ? -MAX + ply : 0;

        Array.Sort(
            Array.ConvertAll(moves, move => {
                if (move == bestMove) return -MAX;
                return -(
                    (move.IsCapture ? 100 * (int)move.CapturePieceType - (int)move.MovePieceType : 0) +
                    (move.IsPromotion ? 5 * (int)move.PromotionPieceType : 0)
                );
            }),
            moves
        );

        foreach (Move move in moves) {
            board.MakeMove(move);
            int score = -Search(depth - 1, ply + 1, -beta, -alpha);
            board.UndoMove(move);

            /*
            if (timer.MillisecondsElapsedThisTurn > timeout) {
                return -MAX;
            }
            */

#if DEBUG
            //Debug(ply, "received score for " + move.ToString() + " = " + score + "; best = " + result);
#endif

            if (score > result) {
                result = score;
                bestMove = move;
                if (ply == 0) {
                    choice = move;
#if DEBUG
                    choiceScore = result;
                    //Debug(0, "A=" + alpha + " B=" + beta + "; found score = " + score + "; updating current: " + choice.ToString());
#endif
                }
                alpha = Math.Max(alpha, score);
                if (alpha >= beta) break;
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