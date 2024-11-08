﻿using LambdaAnaliz.LambdaSyntaxTree;

namespace LambdaAnaliz;

internal class MainClass
{
    public static void Main(string[] args)
    {
        const string lambda_string = @"λx.λy.λz.x z (y z)"; //S combinator
        LambdaExpression lambda_exp = LambdaAnaliz.LambdaParser.ParseTerm(lambda_string);
        Console.WriteLine(lambda_exp.PrintBinary());
    }
}