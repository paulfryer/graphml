using System;
using System.Linq.Expressions;

namespace GraphML.EntityFramework
{
    public Graph(
    Expression<Func<TPredictSource, dynamic>> predictValue,
    Func<TPredictSource, bool> predictFilter)
    {
    PredictValue = predictValue;
    GraphId = $"graph-{DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks:10}";

    if (typeof(ICoded).IsAssignableFrom(typeof(TPredictSource)))
    {
        Expression<Func<TPredictSource, dynamic>> id = f => ((ICoded)f).Code;
        PredictNode = new Node<TPredictNode, TPredictSource>(id, predictFilter);
    }
    else if (typeof(IId).IsAssignableFrom(typeof(TPredictSource)))
    {
    Expression<Func<TPredictSource, dynamic>> id = f => ((IId)f).Id;
    PredictNode = new Node<TPredictNode, TPredictSource>(id, predictFilter);
    }

}
}
