using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SquashMatrixStats {
    /// <summary>
    /// an object that holds all the result data for a single player vs player clash
    /// </summary>
    public class Result {
        private DateTime _date;
        private string _eventRegion = ""; // this is the pennant region
        private string _division = "";
        private string _round = "";
        private int _position;  // position played in the team, not sexual position or anything
        private int _playerGames;
        private int _opponentGames;
        private int _playerPoints;
        private int _opponentPoints;
        private double _ratingAdjustment;  // a double with a + or - infront.  need to see if it can be cast directly to a signed double...
        private double _rating;  // this is the rating after the adjustment
        private string _opponentName = "";  // firstname space last name "Nick Fletcher"
        private string _opponentID = "";
        private double _opponentRating;
        private string _note = "";
        private string _matchResultID = "";

        // i think all these get/set things can be vastly simplified because i don't do any logic on them
        public DateTime date  // returns the date of the match in an actual date object
        {
            get {
                return _date;
            }
            set  // i don't think we'll ever need to set the date with an input of DateTime, but whatevs.
            {
                _date = value;
                //Debug.Print("_date has been parsed to be: " + _date.ToString());
            }
        }

        public string eventRegion {
            get {
                return _eventRegion;
            }
            set {
                _eventRegion = value;
                //Debug.Print("_event is: " + _eventRegion);
            }
        }

        public string division {
            get {
                return _division;
            }
            set {
                _division = value;
                //Debug.Print("_division is: " + _division);
            }
        }

        // can be "Adjustment" 
        public string round {
            get {
                return _round;
            }
            set {
                _round = value;
                //Debug.Print("_round is: " + _round);
            }
        }

        public int position {
            get {
                return _position;
            }
            set {
                _position = value;
                //Debug.Print("_position is: " + _position.ToString());
            }
        }

        public int playerGames {
            get {
                return _playerGames;
            }
            set {
                _playerGames = value;
                //Debug.Print("_playerGames is: " + _playerGames.ToString());
            }
        }

        public int opponentGames {
            get {
                return _opponentGames;
            }
            set {
                _opponentGames = value;
                //Debug.Print("_opponentGames is: " + _opponentGames.ToString());
            }
        }

        public int playerPoints {
            get {
                return _playerPoints;
            }
            set {
                _playerPoints = value;
                //Debug.Print("_playerPoints is: " + _playerPoints.ToString());
            }
        }

        public int opponentPoints {
            get {
                return _opponentPoints;
            }
            set {
                _opponentPoints = value;
                //Debug.Print("_opponentPoints is: " + _opponentPoints.ToString());
            }
        }

        public double ratingAdjustment {
            get {
                return _ratingAdjustment;
            }
            set {
                _ratingAdjustment = value;
                //Debug.Print("_ratingAdjustment is : " + _ratingAdjustment.ToString());
            }
        }

        public double rating {
            get {
                return _rating;
            }
            set {
                _rating = value;
                //Debug.Print("_rating is : " + _rating.ToString());
            }
        }

        public string opponentName {
            get {
                return _opponentName;
            }
            set {
                _opponentName = value;
                //Debug.Print("_opponentName is: " + _opponentName);
            }
        }

        public string opponentID {
            get {
                return _opponentID;
            }
            set {
                _opponentID = value;
                //Debug.Print("_opponentID is: " + _opponentID);
            }
        }

        public double opponentRating {
            get {
                return _opponentRating;
            }
            set {
                _opponentRating = value;
                //Debug.Print("_opponentRating is : " + _opponentRating.ToString());
            }
        }

        // this is definitely optional
        public string note {
            get {
                return _note;
            }
            set {
                _note = value;
            }
        }

        public string matchResultID {
            get {
                return _matchResultID;
            }
            set {
                _matchResultID = value;
                //Debug.Print("_matchResultID is: " + _matchResultID);
            }
        }
    }
}
