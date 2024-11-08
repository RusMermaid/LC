

namespace LambdaAnaliz.LambdaSyntaxTree;

public class LambdaFunction : LambdaTerm
{
    //The "argument," if you will
    public LambdaVariable Input;

    //The output of this function
    public LambdaTerm Output;

    public LambdaFunction(LambdaVariable input, LambdaTerm output)
    {
        input.Parent = this;
        output.Parent = this;

        this.Input = input;
        this.Output = output;
        this.Input.IsDefinition = true;
    }

    
    public override bool BetaReduce()
    {
        try { return this.Output.BetaReduce(); }
        catch { return true; }
    }

    
    public override bool IsBound(string variable)
    {
        if (Input.Name == variable) return true;
        return this.Parent == null ? false : this.Parent.IsBound(variable);
    }

    internal override void Replace(LambdaVariable what, LambdaTerm with)
    {
        this.Output.Replace(what, with);
    }

    public override int GetDeBruijnIndex(string name = "")
    {
        if (name == this.Input.Name)
        {
            return 1;
        }

        int parentDeBruijn = this.Parent.GetDeBruijnIndex(name);
        return parentDeBruijn < 0 ? parentDeBruijn : parentDeBruijn + 1;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns>A nice string representation of the object</returns>
    public override string ToString() => "λ" + this.Input + "." + this.Output;

    public override string PrintDeBruijn() => "λ." + this.Output.PrintDeBruijn();
}