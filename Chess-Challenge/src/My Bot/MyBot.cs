using ChessChallenge.API;
using System;
using static System.Math;

public class MyBot : IChessBot
{

    Move choice;

    Board board;
    Timer timer;

    (Move move, ulong key, int depth, int flag, int score)[] tt = {};
    Move[,] killers;
    int[,,] history;

    int MAX = 10000000, timeout, nodes;

    public MyBot() {
        Array.Resize(ref tt, 0x7FFFFF);
    }

    public Move Think(Board _board, Timer _timer)
    {
        board = _board;
        timer = _timer;
        killers = new Move[60,2];
        history = new int[2, 7, 64];

        nodes = 0;

        // Default to some move, determine our minimum search time.
        choice = board.GetLegalMoves()[0];
        timeout = timer.MillisecondsRemaining / 30;

        // 5ms or less, panic and hope the other bot times out first.
        if (timeout < 5) return choice;

        // Iterative deepening with time constraint.
        int depth = 1, alpha = -MAX, beta = MAX;
        try {
            while (depth <= 30) {
                Search(depth++, 0, alpha, beta);
                /*
                int score = Search(depth, 0, alpha, beta);

                // TODO: tune these values AFTER all other pruning features?
                if (score <= alpha) alpha -= 100;
                else if (score >= beta) beta += 100;
                else {
                    depth++;
                    alpha = score - 25;
                    beta = score + 25;
                }
                */
                //Console.WriteLine("DEPTH = " + (depth-1) + "; nodes = " + nodes + "; Choice " + choice.ToString());
            }
        } catch {}

        // Return our best result.
        return choice;
    }

    int Search(int depth, int ply, int alpha, int beta, bool canNullMove = true) {

        bool quiesce = depth <= 0,
             notRoot = ply > 0, 
             isPvNode = alpha != beta - 1,
             isInCheck = board.IsInCheck();
        ulong key = board.ZobristKey;
        int oAlpha, result = -MAX, moveCount = 0, score;

        nodes++;

        int DoSearch(int newDepth, int newAlpha, int reduction = 0, bool canDoNull = true) => 
            score = -Search(newDepth - reduction, ply + 1, -newAlpha, -alpha, canDoNull);

        // Handle timeout scenario.
        if (timer.MillisecondsElapsedThisTurn > timeout) ply /= 0;

        // Handle repeated move draw scenario.
        if (notRoot && board.IsRepeatedPosition()) return 0;

        // Take a look in the transposition table, see if we can return early.
        var ttEntry = tt[key % 0x7FFFFF];
        if (notRoot && ttEntry.key == key && ttEntry.depth >= depth && (
                ttEntry.flag == 0 
                || (ttEntry.flag == 1 && ttEntry.score <= alpha) 
                || (ttEntry.flag == 2 && ttEntry.score >= beta)
            )) return ttEntry.score;

        // Internal iterative reduction.
        //if (isPvNode && depth >= 4  && ttEntry.move.IsNull) depth--;

        // Otherwise take the transposition entry as our best move, even if it
        // might not exist.
        Move bestMove = ttEntry.move;

        // Handle quiescent inside of our search function to save tokens.
        int eval = Evaluate();
        if (quiesce) {
            result = eval;
            if (result >= beta) return result;
            alpha = Max(alpha, result);
        }
        else if (!isPvNode && !isInCheck) {

            // Reverse futility pruning.
            //int rfpMargin = 60 * depth;
            //if (depth <= 4 && eval - rfpMargin >= beta) return eval - rfpMargin;
            //*/

            // depth <= 6
            // 60: +8.0  +- 17.6 

            // depth <= 5
            // 95 - 425429
            // 85 - 403614 -29.3 +- 36.4
            // 75 - 388912 -22.3 +- 37.5
            // 65 -        -9.7  +- 35.7
            // 60 -        +9.7  +- 18.1
            // 55 -        -20.9 +- 37.8
            // 50 -        -9.7  +- 36.1
            // 45 - 316207 +
            // 0  - 142633

            // depth <= 4
            // 60: +11.5 +- 17.6

            /* Null move pruning.
            if (canNullMove && depth > 1) {
                board.TrySkipTurn();
                int nullMoveScore = -Search(depth - 3 - depth / 3, ply + 1, -beta, -alpha, false);
                board.UndoSkipTurn();

                if (nullMoveScore >= beta) return nullMoveScore;
            }
            //*/
        }

        // Save our original alpha for after we process our moves.
        oAlpha = alpha;

        // Get moves, captures only if we're in quiescent search and we're not in check.
        Move[] moves = board.GetLegalMoves(quiesce && !isInCheck);

        // If we're in regular search and there are no moves, it's a draw if there is no
        // check and we've lost if there is a check.
        if (!quiesce && moves.Length == 0) return isInCheck ? ply - MAX : 0;

        // Generate our move ordering weights for our current move selection.
        Array.Sort(
            Array.ConvertAll(moves, move => {
                // If our transposition table move is found, weight it the highest.
                if (move == bestMove) return -24_000_000;

                // If the move is a capture, weight it by the captured piece and then by the
                // capturing piece.
                if (move.IsCapture) return (int)move.MovePieceType - 4_000_000 * (int)move.CapturePieceType;

                // If the move is a killer move, weight it beneath all capturing/promoting moves.
                if (killers[ply, 0] == move || killers[ply, 1] == move) return -2_000_000;

                // Otherwise return the history heuristic for this move.
                return -history[ply & 1, (int)move.MovePieceType, move.TargetSquare.Index];
            }),
            moves
        );

        // Loop over our legal moves.
        foreach (Move move in moves) {

            board.MakeMove(move);
            moveCount++;

            int newDepth = board.IsInCheck() ? depth : depth - 1,
                reduction = moveCount/8 + (isPvNode ? 0 : 1);

            if (
                // If this is the first move or we're in quiescence, only do a full search.
                moveCount == 0 || quiesce || 
                (
                    // If in the right conditions, try a null window reduced search first.
                    moveCount < 4 || depth <= 2 ||
                    DoSearch(newDepth, alpha + 1, reduction) > alpha
                )
                // If we were above alpha, try a null window search without reduction.
                && DoSearch(newDepth, alpha + 1) > alpha
            )
                // If we skipped special searches or achieved a better alpha, try a full search.
                DoSearch(newDepth, beta);

            board.UndoMove(move);

            // Update our bests if this move is the best we've seen.
            if (score > result) {
                result = score;

                // Update alpha with the new score, save move if root.
                if (score > alpha) {
                    alpha = score;
                    bestMove = move;

                    // If we're on the first ply (root), save this best move.
                    if (!notRoot) choice = move;
                }

                // Handle beta cutoff.
                if (alpha >= beta) {

                    // If the move is quiet, update killers and history heuristic.
                    if (!move.IsCapture) {
                        if (killers[ply, 0] != move) {
                            killers[ply, 1] = killers[ply, 0];
                            killers[ply, 0] = move;
                        }
                        history[ply & 1, (int)move.MovePieceType, move.TargetSquare.Index] += depth * depth;
                    }

                    // Beta cutoff.
                    break;
                }
            }
        }

        // If we have a best move, save it in the transposition table.
        tt[key % 0x7FFFFF] = (
            bestMove,
            key,
            depth,
            // Determine the flag based on the best result we have. 
            result >= beta ? 2 : result > oAlpha ? 0 : 1,
            result
        );

        return result;
    }    

    /*
        Evaluation based on evaluation from Tier 2 and Tyrant.
    */
    int Evaluate() {
        int mg = 0, eg = 0, phase = 0, piece, ind;

        foreach(int stm in new[] {56, 0}) {
            for (piece = -1; ++piece < 6;) {
                ulong mask = board.GetPieceBitboard((PieceType)piece + 1, stm == 56);
                while(mask != 0) {
                    phase += 0x00042110 >> piece * 4 & 0x0F;
                    ind = 128 * piece + BitboardHelper.ClearAndGetIndexOfLSB(ref mask) ^ stm;
                    mg += getPstVal(ind) + piece_values[piece];
                    eg += getPstVal(ind + 64) + piece_values[piece + 6];
                }
            }

            mg = -mg;
            eg = -eg;
        }

        return (mg * phase + eg * (24 - phase)) / 24 * (board.IsWhiteToMove ? 1 : -1) + phase / 2;
    }

    int getPstVal(int psq) {
        return (int)(((psts[psq / 10] >> (6 * (psq % 10))) & 63) - 20) * 8;
    }

    ulong[] psts = {657614902731556116, 420894446315227099, 384592972471695068, 312245244820264086, 364876803783607569, 366006824779723922, 366006826859316500, 786039115310605588, 421220596516513823, 366011295806342421, 366006826859316436, 366006896669578452, 162218943720801556, 440575073001255824, 657087419459913430, 402634039558223453, 347425219986941203, 365698755348489557, 311382605788951956, 147850316371514514, 329107007234708689, 402598430990222677, 402611905376114006, 329415149680141460, 257053881053295759, 291134268204721362, 492947507967247313, 367159395376767958, 384021229732455700, 384307098409076181, 402035762391246293, 328847661003244824, 365712019230110867, 366002427738801364, 384307168185238804, 347996828560606484, 329692156834174227, 365439338182165780, 386018218798040211, 456959123538409047, 347157285952386452, 365711880701965780, 365997890021704981, 221896035722130452, 384289231362147538, 384307167128540502, 366006826859320596, 366006826876093716, 366002360093332756, 366006824694793492, 347992428333053139, 457508666683233428, 329723156783776785, 329401687190893908, 366002356855326100, 366288301819245844, 329978030930875600, 420621693221156179, 422042614449657239, 384602117564867863, 419505151144195476, 366274972473194070, 329406075454444949, 275354286769374224, 366855645423297932, 329991151972070674, 311105941360174354, 256772197720318995, 365993560693875923, 258219435335676691, 383730812414424149, 384601907111998612, 401758895947998613, 420612834953622999, 402607438610388375, 329978099633296596, 67159620133902};
    int[] piece_values = { 82, 337, 365, 477, 1025, 10000, 94, 281, 297, 512, 936, 10000 };
}