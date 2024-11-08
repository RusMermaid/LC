
namespace LambdaAnaliz.LambdaSyntaxTree;


public class TermDefinition : LambdaTerm
{
    public string Name;
    private bool isFirstDefinition;

    public TermDefinition(string name, string term)
    {
        this.Name = name;

        this.isFirstDefinition = true;


        //RuntimeEnvironment.Terms.Add(name, term);
    }

    
    public override string ToString() => (!this.isFirstDefinition ? "re" : "") + "defined " + this.Name;
}