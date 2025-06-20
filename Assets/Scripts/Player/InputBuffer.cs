using UnityEngine;
using System.Collections.Generic;

public class InputBuffer : MonoBehaviour
{
    [System.Serializable]
    public class BufferedInput
    {
        public string inputName;
        public float timestamp;
        public float bufferTime;

        public BufferedInput(string inputName, float bufferTime)
        {
            this.inputName = inputName;
            this.timestamp = Time.time;
            this.bufferTime = bufferTime;
        }

        public bool IsExpired()
        {
            return Time.time > timestamp + bufferTime;
        }
    }

    private List<BufferedInput> bufferedInputs = new List<BufferedInput>();

    public void BufferInput(string inputName, float bufferTime)
    {
        // Remove any existing inputs of the same type
        bufferedInputs.RemoveAll(input => input.inputName == inputName);
        
        // Add new input
        bufferedInputs.Add(new BufferedInput(inputName, bufferTime));
    }

    public bool ConsumeInput(string inputName)
    {
        for (int i = bufferedInputs.Count - 1; i >= 0; i--)
        {
            var input = bufferedInputs[i];
            
            if (input.IsExpired())
            {
                bufferedInputs.RemoveAt(i);
                continue;
            }

            if (input.inputName == inputName)
            {
                bufferedInputs.RemoveAt(i);
                return true;
            }
        }
        return false;
    }

    public void ClearBuffer()
    {
        bufferedInputs.Clear();
    }
} 