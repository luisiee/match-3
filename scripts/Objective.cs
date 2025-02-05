public class Objective
{
    public bool Completed;

    public Objective(bool completed = false)
    {
        Completed = completed;
    }
}

public class ScoreObjective : Objective
{
    public readonly int SCORE;

    public ScoreObjective(int score, bool completed = false) : base(completed)
    {
        SCORE = score;
    }
}
