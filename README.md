## The problem
Normally when you're trying to modify expression trees you need to write an expression visitor.
When trying to plug your expression visitor into an IQuerable's expression tree, you need to write a linq provider (yikes).

## The solution
QueryInterceptor introduces one extension method on IQueryable<T> (InterceptWith) that lets you plug in arbitrary expression visitors.

    namespace QueryInterceptor {
        public static class QueryableExtensions {
            public static IQueryable<T> InterceptWith<T>(this IQueryable<T> source, params ExpressionVisitor[] visitors);
        }
    }

## Basic Example
The example below uses an expression visitor that changes == to != anywhere in the expression tree.

    public class EqualsToNotEqualsVisitor : ExpressionVisitor {
        protected override Expression VisitBinary(BinaryExpression node) {
            if (node.NodeType == ExpressionType.Equal) {
                // Change == to !=
                return Expression.NotEqual(node.Left, node.Right);
            }
            return base.VisitBinary(node);
        }
    }
    
    class Program {
        static void Main(string[] args) {
            var query = from n in Enumerable.Range(0, 10).AsQueryable()
                        where n % 2 == 0
                        select n;

            // Print even numbers
            foreach (var item in query) {
                Console.WriteLine(item);
            }

            Console.WriteLine();

            // Print odd numbers
            var visitor = new EqualsToNotEqualsVisitor();
            foreach (var item in query.InterceptWith(visitor)) {
                Console.WriteLine(item);
            }
        }
    }
    