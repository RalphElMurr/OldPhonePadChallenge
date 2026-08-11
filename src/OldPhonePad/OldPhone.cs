namespace OldPhonePad;

public static class OldPhone
{
    //this is what gets called, takes us to the decode function
    //we use .default to say that im using this object, this type of layout
    public static string OldPhonePad(string input) => OldPhonePadDecoder.Default.Decode(input);
}
