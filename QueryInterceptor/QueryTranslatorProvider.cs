using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace QueryInterceptor {
    internal class QueryTranslatorProvider<T> : ExpressionVisitor, IQueryProvider {
        private readonly IQueryable _source;
        private readonly IEnumerable<ExpressionVisitor> _visitors;

        public QueryTranslatorProvider(IQueryable source, IEnumerable<ExpressionVisitor> visitors) {
            if (source == null) {
                throw new ArgumentNullException("source");
            }
            if (visitors == null) {
                throw new ArgumentNullException("visitors");
            }
            _visitors = visitors;
            _source = source;
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression) {
            if (expression == null) {
                throw new ArgumentNullException("expression");
            }

            return new QueryTranslator<TElement>(_source, expression, _visitors) as IQueryable<TElement>;
        }

        public IQueryable CreateQuery(Expression expression) {
            if (expression == null) {
                throw new ArgumentNullException("expression");
            }

            Type elementType = expression.Type.GetGenericArguments().First();
            IQueryable result = (IQueryable)Activator.CreateInstance(typeof(QueryTranslator<>).MakeGenericType(elementType),
                    new object[] { _source, expression, _visitors });
            return result;
        }

        public TResult Execute<TResult>(Expression expression) {
            if (expression == null) {
                throw new ArgumentNullException("expression");
            }
            object result = (this as IQueryProvider).Execute(expression);
            return (TResult)result;
        }

        public object Execute(Expression expression) {
            if (expression == null) {
                throw new ArgumentNullException("expression");
            }

            Expression translated = VisitAll(expression);
            return _source.Provider.Execute(translated);
        }

        internal IEnumerable ExecuteEnumerable(Expression expression) {
            if (expression == null) {
                throw new ArgumentNullException("expression");
            }

            Expression translated = VisitAll(expression);
            return _source.Provider.CreateQuery(translated);
        }

        private Expression VisitAll(Expression expression) {
            // Run all visitors in order
            var visitors = new ExpressionVisitor[] { this }.Concat(_visitors);

            return visitors.Aggregate<ExpressionVisitor, Expression>(expression, (expr, visitor) => visitor.Visit(expr));
        }

        protected override Expression VisitConstant(ConstantExpression node) {
            // Fix up the Expression tree to work with the underlying LINQ provider
            if (node.Type.IsGenericType &&
                node.Type.GetGenericTypeDefinition() == typeof(QueryTranslator<>)) {
                return _source.Expression;
            }

            return base.VisitConstant(node);
        }
    }
}
