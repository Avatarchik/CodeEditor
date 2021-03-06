using System;
using System.Collections.Generic;
using System.Concurrency;
using System.Linq;

namespace CodeEditor.Reactive
{
	public interface IObservableX<out T>
	{
		IDisposable Subscribe(IObserverX<T> observer);
	}

	public interface IObserverX<in T>
	{
		void OnNext(T value);
		void OnError(Exception exception);
		void OnCompleted();
	}

	public static class ObserverX
	{
		public static void CompleteWith<T>(this IObserverX<T> observer, T value)
		{
			observer.OnNext(value);
			observer.OnCompleted();
		}
	}

	public static class ObservableX
	{
		public static IObservableX<T> ObserveOnThreadPool<T>(this IObservableX<T> source)
		{
			return source.Map(_ => _.ObserveOn(Scheduler.ThreadPool));
		}

		public static IObservableX<T> SubscribeOnThreadPool<T>(this IObservableX<T> source)
		{
			return source.Map(_ => _.SubscribeOn(Scheduler.ThreadPool));
		}

		public static IObservableX<T> Empty<T>()
		{
			return Observable.Empty<T>().ToObservableX();
		}

		public static IObservableX<T> Start<T>(Func<T> func)
		{
			return Observable.Start(func).ToObservableX();
		}

		/// <summary>
		/// Delays the observable sequence by <paramref name="dueTime"/>.
		/// 
		/// OnError notifications ARE NOT DELAYED!
		/// </summary>
		public static IObservableX<T> Delay<T>(this IObservableX<T> source, TimeSpan dueTime)
		{
			return source.Map(_ => _.Delay(dueTime));
		}

		public static IObservableX<T> Repeat<T>(this IObservableX<T> source)
		{
			return source.Map(_ => _.Repeat());
		}

		public static IObservableX<T> Repeat<T>(this IObservableX<T> source, int repeatCount)
		{
			return source.Map(_ => _.Repeat(repeatCount));
		}

		public static IObservableX<T> Retry<T>(this IObservableX<T> source)
		{
			return source.Map(_ => _.Retry());
		}

		public static IObservableX<T> Retry<T>(this IObservableX<T> source, int retryCount)
		{
			return source.Map(_ => _.Retry(retryCount));
		}

		public static IObservableX<T> RetryEvery<T>(this IObservableX<T> source, TimeSpan retryPeriod, int retryCount)
		{
			return source.CatchAndDelayRethrowBy(retryPeriod).Retry(retryCount);
		}

		public static IObservableX<T> CatchAndDelayRethrowBy<T>(this IObservableX<T> source, TimeSpan dueTime)
		{
			return source.Catch((Exception e) => ThrowAfterTimeout<T>(dueTime, e));
		}

		public static IObservableX<T> ThrowAfterTimeout<T>(TimeSpan dueTime, Exception e)
		{
			return Never<T>().Timeout(dueTime, Throw<T>(e));
		}

		public static IObservableX<T> Return<T>(T value)
		{
			return Observable.Return(value).ToObservableX();
		}

		public static IObservableX<T> Never<T>()
		{
			return Observable.Never<T>().ToObservableX();
		}

		public static IObservableX<T> Throw<T>(Exception exception)
		{
			return Observable.Throw<T>(exception).ToObservableX();
		}

		public static IObservableX<T> Catch<T>(this IObservableX<T> source, IObservableX<T> second)
		{
			return source.Map(_ => _.Catch(second.ToObservable()));
		}

		public static IObservableX<T> Catch<T, TException>(this IObservableX<T> source, Func<TException, IObservableX<T>> handler) where TException : Exception
		{
			return source.Map(_ => _.Catch((TException exception) => handler(exception).ToObservable()));
		}

		public static IDisposable Subscribe<T>(this IObservableX<T> source, Action<T> onNext)
		{
			return source.ToObservable().Subscribe(onNext);
		}

		public static IDisposable Subscribe<T>(this IObservableX<T> source, Action<T> onNext, Action<Exception> onError)
		{
			return source.ToObservable().Subscribe(onNext, onError);
		}

		public static IDisposable Subscribe<T>(this IObservableX<T> source, Action<T> onNext, Action<Exception> onError, Action onCompleted)
		{
			return source.ToObservable().Subscribe(onNext, onError, onCompleted);
		}

		public static IObservableX<TResult> Select<T, TResult>(this IObservableX<T> source, Func<T, TResult> selector)
		{
			return source.Map(_ => _.Select(selector));
		}

		public static IObservableX<TResult> SelectMany<T, TResult>(this IObservableX<T> source, Func<T, IEnumerable<TResult>> selector)
		{
			return source.Map(_ => _.SelectMany(selector));
		}

		public static IObservableX<TResult> SelectMany<T, TResult>(this IObservableX<T> source, Func<T, IObservableX<TResult>> selector)
		{
			Func<T, IObservable<TResult>> observableSelector = t => selector(t).ToObservable();
			return source.Map(_ => _.SelectMany(observableSelector));
		}
		
		public static IObservableX<TResult> SelectMany<TSource, TCollection, TResult>(this IObservableX<TSource> source, Func<TSource, IObservableX<TCollection>> collectionSelector, Func<TSource, TCollection, TResult> resultSelector)
		{
			Func<TSource, IObservable<TCollection>> observableCollectionSelector = t => collectionSelector(t).ToObservable();
			return source.Map(_ => _.SelectMany(observableCollectionSelector, resultSelector));
		}

		public static IObservableX<T> Where<T>(this IObservableX<T> source, Func<T, bool> predicate)
		{
			return source.Map(_ => _.Where(predicate));
		}

		public static IObservableX<T> TakeWhile<T>(this IObservableX<T> source, Func<T, bool> predicate)
		{
			return source.Map(_ => _.TakeWhile(predicate));
		}

		public static IObservableX<T> Take<T>(this IObservableX<T> source, int count)
		{
			return source.Map(_ => _.Take(count));
		}

		public static IObservableX<T> Do<T>(this IObservableX<T> source, Action<T> action)
		{
			return source.Map(_ => _.Do(action));
		}

		public static IObservableX<T> Merge<T>(this IEnumerable<IObservableX<T>> sources)
		{
			return sources.Select(_ => _.ToObservable()).Merge().ToObservableX();
		}

		public static T FirstOrDefault<T>(this IObservableX<T> source)
		{
			return source.ToObservable().FirstOrDefault();
		}

		public static T FirstOrTimeout<T>(this IObservableX<T> source, TimeSpan timeout)
		{
			return source.ToObservable().Timeout(timeout).First();
		}

		public static IObservableX<T> Timeout<T>(this IObservableX<T> source, TimeSpan timeout)
		{
			return source.Map(_ => _.Timeout(timeout));
		}

		public static IObservableX<T> Timeout<T>(this IObservableX<T> source, TimeSpan timeout, IObservableX<T> other)
		{
			return source.Map(_ => _.Timeout(timeout, other.ToObservable()));
		}

		public static IObservableX<IList<T>> ToList<T>(this IObservableX<T> source)
		{
			return source.Map(_ => _.ToList());
		}

		public static IObservableX<T> ToObservableX<T>(this IEnumerable<T> source)
		{
			return source.ToObservable().ToObservableX();
		}

		public static IObservableX<TResult> Map<T, TResult>(this IObservableX<T> source, Func<IObservable<T>, IObservable<TResult>> selector)
		{
			return selector(source.ToObservable()).ToObservableX();
		}

		public static IEnumerable<T> ToEnumerable<T>(this IObservableX<T> source)
		{
			return source.ToObservable().ToEnumerable();
		}

		public static IObservableX<T> Using<T, TResource>(Func<TResource> resourceSelector, Func<TResource, IObservableX<T>> resourceUsage) where TResource : IDisposable
		{
			return Observable.Using(resourceSelector, resource => resourceUsage(resource).ToObservable()).ToObservableX();
		}

		public static IObservableX<TResult> Generate<TState, TResult>(TState initialState, Func<TState, bool> condition, Func<TState, TState> iteration, Func<TState, TResult> selection)
		{
			return Observable.Generate(initialState, condition, iteration, selection).ToObservableX();
		}

		public static IObservableX<T> Create<T>(Func<IObserverX<T>, Action> subscribe)
		{
			return Observable.Create<T>(observer => subscribe(observer.ToObserverX())).ToObservableX();
		}

		public static IObservableX<T> CreateWithDisposable<T>(Func<IObserverX<T>, IDisposable> subscribe)
		{
			return Observable.CreateWithDisposable<T>(observer => subscribe(observer.ToObserverX())).ToObservableX();
		}

		public static Func<IObservableX<TResult>> FromAsyncPattern<TResult>(Func<AsyncCallback, object, IAsyncResult> begin, Func<IAsyncResult, TResult> end)
		{
			var fromAsyncPattern = Observable.FromAsyncPattern(begin, end);
			return () => fromAsyncPattern().ToObservableX();
		}

		public static IObservableX<TResult> Defer<TResult>(Func<IObservableX<TResult>> observableFactory)
		{
			return Observable.Defer(() => observableFactory().ToObservable()).ToObservableX();
		}
	}
}
