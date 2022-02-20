vec4 color4Combine(vec4 one, vec4 two)
{        
    if(one.w == 0 && two.w == 0)
    {
        return vec4(0, 0, 0, 1);
    }
    if(one.w == 0)
    {
        return two;
    }
    if(two.w == 0)
    {
        return one;
    }

    //TODO: delete this file?
    return mix(one, two, (one.w + two.w) / 2.0); // vec4((one.xyz * one.w) + (two.xyz * two.w), (one.w + two.w) / 2.0);
}