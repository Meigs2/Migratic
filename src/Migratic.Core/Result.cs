using System;

namespace Migratic.Core
{
    public class Result<TResult, TError>
    {
        private bool _isSuccess;
        public bool IsSuccess => _isSuccess;
        
        private TResult _value;
        public TResult Value =>
            _isSuccess
                ? _value
                : throw new InvalidResultValueAccessException(
                    "An attempt was made to access the 'Value' of a Result when it is not a Success.");

        private TError _error;
        public TError Error =>
            !_isSuccess
                ? _error
                : throw new InvalidResultErrorAccessException(
                    "An attempt was made to access the 'Error' of an Result when it a Success.");

        public Result(TResult value)
        {
            _isSuccess = true;
            _value = value;
        }

        public Result(TError error)
        {
            _isSuccess = false;
            _error = error;
        }
    }
}



public class InvalidResultErrorAccessException : Exception
{
    internal InvalidResultErrorAccessException(string message) : base(message)
    {
    }
}

public class InvalidResultValueAccessException : Exception
{
    internal InvalidResultValueAccessException(string message) : base(message)
    {
    }
}
