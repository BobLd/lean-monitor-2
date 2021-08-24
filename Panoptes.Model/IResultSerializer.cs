﻿namespace Panoptes.Model
{
    public interface IResultSerializer
    {
        Result Deserialize(string serializedResult);
        string Serialize(Result result);
    }
}
