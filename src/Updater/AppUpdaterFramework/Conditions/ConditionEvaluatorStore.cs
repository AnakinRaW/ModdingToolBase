using System;
using System.Collections.Generic;
namespace AnakinRaW.AppUpdaterFramework.Conditions;

internal sealed class ConditionEvaluatorStore : IConditionEvaluatorStore
{
    private readonly IDictionary<ConditionType, IConditionEvaluator> _conditionEvaluators =
        new Dictionary<ConditionType, IConditionEvaluator>();

    public void AddConditionEvaluator(IConditionEvaluator evaluator)
    {
        if (evaluator == null) 
            throw new ArgumentNullException(nameof(evaluator));
        _conditionEvaluators[evaluator.Type] = evaluator;
    }

    public IConditionEvaluator? GetConditionEvaluator(ICondition? condition)
    {
        if (condition == null)
            return null;
        _conditionEvaluators.TryGetValue(condition.Type, out var conditionEvaluator);
        return conditionEvaluator;
    }
}