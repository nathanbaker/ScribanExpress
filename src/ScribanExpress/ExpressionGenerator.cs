using Scriban.Syntax;
using ScribanExpress.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace ScribanExpress
{
    public class ExpressionGenerator
    {
        public ExpressionGenerator()
        {
        }

        public Expression<Action<StringBuilder, T, Y>> Generate<T, Y>(ScriptBlockStatement scriptBlockStatement)
        {
            ParameterExpression StringBuilderParmeter = Expression.Parameter(typeof(StringBuilder));
            ParameterExpression InputParameter = Expression.Parameter(typeof(T));
            ParameterExpression LibraryParameter = Expression.Parameter(typeof(Y));

            ParameterFinder parameterFinder = new ParameterFinder();
            parameterFinder.AddType(LibraryParameter);
            parameterFinder.AddType(InputParameter);

            List<Expression> statements = new List<Expression>();
            var appendMethodInfo = typeof(StringBuilder).GetMethod("Append", new[] { typeof(string) });

            foreach (var statement in scriptBlockStatement.Statements)
            {
                switch (statement)
                {
                    case ScriptRawStatement scriptRawStatement:
                        var constant = Expression.Constant(scriptRawStatement.ToString());
                        var methodCall = Expression.Call(StringBuilderParmeter, appendMethodInfo, constant);
                        statements.Add(methodCall);
                        break;
                    case ScriptExpressionStatement scriptExpressionStatement:
                        var expressionBody = GetExpressionBody(scriptExpressionStatement.Expression, parameterFinder, null);
                        expressionBody = AddToString(expressionBody);
                        var scriptmethodCall = Expression.Call(StringBuilderParmeter, appendMethodInfo, expressionBody);
                        statements.Add(scriptmethodCall);
                        break;
                    case ScriptIfStatement scriptIfStatement:
                        var predicateExpression = GetExpressionBody(scriptIfStatement.Condition, parameterFinder, null);

                        var trueStatementBlock = GetStatementBlock(StringBuilderParmeter, scriptIfStatement.Then, parameterFinder);

                        if (scriptIfStatement.Else != null)
                        {
                            var elseStatment = (scriptIfStatement.Else as ScriptElseStatement);
                            var falseStatementBlock = GetStatementBlock(StringBuilderParmeter, elseStatment.Body, parameterFinder);
                            ConditionalExpression ifThenElseExpr = Expression.IfThenElse(predicateExpression, trueStatementBlock, falseStatementBlock);
                            statements.Add(ifThenElseExpr);
                        }
                        else
                        {
                            ConditionalExpression ifThenExpr = Expression.IfThen(predicateExpression, trueStatementBlock);
                            statements.Add(ifThenExpr);
                        }
                        
                        break;
                }
            }
            var blockExpression = Expression.Block(statements);
            return Expression.Lambda<Action<StringBuilder, T, Y>>(blockExpression, StringBuilderParmeter, InputParameter, LibraryParameter);
        }



        public BlockExpression GetStatementBlock(ParameterExpression stringBuilderParameter,ScriptBlockStatement scriptBlockStatement, ParameterFinder parameterFinder)
        {
            var appendMethodInfo = typeof(StringBuilder).GetMethod("Append", new[] { typeof(string) });

            List<Expression> statements = new List<Expression>();
            foreach (var statement in scriptBlockStatement.Statements)
            {
                switch (statement)
                {
                    case ScriptRawStatement scriptRawStatement:
                        var constant = Expression.Constant(scriptRawStatement.ToString());
                        var methodCall = Expression.Call(stringBuilderParameter, appendMethodInfo, constant);
                        statements.Add(methodCall);
                        break;
                }
            }
            var blockExpression = Expression.Block(statements);
            return blockExpression;
        }

        public Expression GetExpressionBody(ScriptExpression scriptExpression, ParameterFinder parameterFinder, List<Expression> arguments)
        {
            Expression currentExpression = null;
            switch (scriptExpression)
            {
                case ScriptVariableGlobal scriptVariableGlobal:
                    var parameter = parameterFinder.Find(scriptVariableGlobal.Name);
                    currentExpression = Expression.Property(parameter, scriptVariableGlobal.Name);
                    break;

                case ScriptLiteral scriptLiteral:
                    currentExpression = Expression.Constant(scriptLiteral.Value, scriptLiteral.Value.GetType());
                    break;

                case ScriptMemberExpression scriptMemberExpression:
                    // it's impossible to tell if we have a member or a method, so we check for both
                    var memberTarget = GetExpressionBody(scriptMemberExpression.Target, parameterFinder, null);
                    var memberName = scriptMemberExpression.Member.Name;
                    var property = ExpressionHelpers.GetProperty(memberTarget.Type, memberName);
                    if (property != null)
                    {
                        currentExpression = Expression.Property(memberTarget, memberName);
                    }


                    // TODO: we should remove the need to calculate a method with Args here, should not need to pass down info
                    // still need the argument list as ScriptPipeCall still needs to pass the args in
                    var argumentTypeList = arguments?.Select(e => e.Type) ?? Enumerable.Empty<Type>();
                    var methodInfo = ExpressionHelpers.GetMethod(memberTarget.Type, memberName, argumentTypeList);

                    if (methodInfo != null)
                    {
                        currentExpression = ExpressionHelpers.CallMethod(methodInfo, memberTarget, arguments);
                    }

                    break;

                case ScriptPipeCall scriptPipeCall:
                    // I'm not a huge fan of this because it requires pushing args down to sub nodes, could cause issues with multi funtions tree
                    var fromExpression = GetExpressionBody(scriptPipeCall.From, parameterFinder, null);
                    // prepare args (input type + ScriptFunctionCall args )
                    var pipelineArgs = new List<Expression> { fromExpression };

                    switch (scriptPipeCall.To)
                    {
                        case ScriptFunctionCall scriptFunctionCall:
                            pipelineArgs.AddRange(scriptFunctionCall.Arguments.Select(arg => GetExpressionBody(arg, parameterFinder, null)));
                            currentExpression = GetExpressionBody(scriptFunctionCall.Target, parameterFinder, pipelineArgs);
                            break;
                        case ScriptMemberExpression scriptMemberExpression:
                            currentExpression = GetExpressionBody(scriptMemberExpression, parameterFinder, pipelineArgs);
                            break;
                        case ScriptPipeCall toScriptPipeCall:
                            var nestedfromExpression = GetExpressionBody(toScriptPipeCall.From, parameterFinder, pipelineArgs);
                            currentExpression = GetExpressionBody(toScriptPipeCall.To, parameterFinder, new List<Expression> { nestedfromExpression });
                            break;

                            // todo break on default
                    }

                    break;
                case ScriptFunctionCall scriptFunctionCall:
                    currentExpression = CalculateScriptFunctionCall(scriptFunctionCall, parameterFinder, arguments);
                    break;
                case ScriptNestedExpression scriptNestedExpression:
                    currentExpression = GetExpressionBody(scriptNestedExpression.Expression, parameterFinder, arguments);
                    break;
            }

            return currentExpression;
        }

        public Expression CalculateScriptFunctionCall(ScriptFunctionCall scriptFunctionCall, ParameterFinder parameterFinder, List<Expression> arguments)
        {
            var args = scriptFunctionCall.Arguments.Select(arg => GetExpressionBody(arg, parameterFinder, null));
            //add first argument if it's being supplied (probably from ScriptPipeCall)
            if (arguments != null)
            {
                args = arguments.Union(args);
            }

            // we are attempting to pull the bottom target member up, so we know the method Name
            var toMember = scriptFunctionCall.Target as ScriptMemberExpression;
            if (toMember == null)
            {
                throw new NotSupportedException();
            }
            else
            {
                var argumentTypes = args?.Select(e => e.Type) ?? Enumerable.Empty<Type>();
                var targetType = GetExpressionBody(toMember.Target, parameterFinder, args.ToList<Expression>());
                var functionName = toMember.Member;
                var methodInfo = ExpressionHelpers.GetMethod(targetType.Type, functionName.Name, argumentTypes.ToArray());
                return ExpressionHelpers.CallMethod(methodInfo, targetType, args);
            }
        }

        public Expression AddToString(Expression input) => (input.Type != typeof(string)) ? Expression.Call(input, "ToString", null, null) : input;
    }
}
