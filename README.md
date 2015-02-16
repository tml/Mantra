# Mantra, a toy language

<p> <strong>Tiny</strong>&#160; Mantra has a simple data and evaluation model. This simplicity
makes it trivial to evaluate and understand expressions in your head, making debugging and 
understanding code easy.
</p>
<p> <strong>Visible</strong>&#160; It's impossible to hide state in the Mantra core. Even the 
equivalent of the function stack is visible in Mantra. It's possible to execute expressions and view every step of the process, pinpointing exactly where a bug takes effect. No longer will 
you be blocked by a debugger being unable to pinpoint exactly where a function call is coming
from.
</p>

<h2> Example </h2>
<p>
Mantra reads superficially like Haskell, though when working with anything deeper than
a basic function its semantics differ massively. 
</p>
<p>
This is a tiny declaration file which
solves problem 14 of the <a href="http://www.ic.unicamp.br/~meidanis/courses/mc336/2006s2/funcional/L-99_Ninety-Nine_Lisp_Problems.html">99 lisp problems</a>
(duplicate each element in a list).
</p>
```
duplicateElem [] => [];
duplicateElem [x .. xs] => cat [x x] duplicateElem xs;
```
<p>
This is how it evaluates, if we skip the steps involved in take and skip.
</p>
```
duplicateElem [1 2 3]
--> cat [1 1] duplicateElem [2 3]
--> cat [1 1] cat [2 2] duplicateElem [3]
--> cat [1 1] cat [2 2] cat [3 3] duplicateElem []
--> cat [1 1] cat [2 2] cat [3 3] []
--> cat [1 1] cat [2 2] [3 3]
--> cat [1 1] [2 2 3 3]
--> [1 1 2 2 3 3]
```
<p>
A small number of simple rules guide this evaluation process. Mantra is what is known as
a "rewrite system". This means that Mantra uses basic rules to continually rewrite 
expressions until there are no rules that apply. To do this, you (or the interpreter)
can follow these instructions:
</p>
<ol>
<li>Read from right to left until you hit a literal which is not in a list (call this RL).</li>
<li>Look up the rule named by the literal RL (call this R). If it doesn't refer to a rule, halt.</li>
<li>Attempt to match each of R's patterns in order with the terms after RL until one succeeds. If all matches fails, halt.</li>
<li>Go through the right side of R, replacing any parameter with the matched argument.</li>
</ol>
<p>
Congratulations! You now know the Mantra evaluation model minus its pattern matching
(which is fairly simple). There is more to it when fibers are involved, but this is all
you need to evaluate expressions.
</p>
<h2> Example improved </h2>
<p>
You might have noticed that the stack grew with every expansion of our duplicateElem rule.
This is because the rule isn't tail recursive. It turns out that in mantra it's incredibly
easy to tell whether a rule is tail recursive. If a the rule itself is the first literal
in the rule definition, theny it's tail recursive. Otherwise it isn't. This is because
that means the rule will be the last part of the body to be evaluated.
</p>
<p>
Let's make the rule tail recursive:
</p>
```
duplicateElemAcc acc [] => acc;
duplicateElemAcc acc [x .. xs] => duplicateElemAcc cat acc [x x] xs;
duplicateElem => duplicateElemAcc [];
```
<p>
Now the rule will use constant stack space when evaluating, rather than rapidly growing 
the stack. This is what evaluation will look like:
</p>
```
duplicateElem [1 2 3]
--> duplicateElemAcc [] [1 2 3]
--> duplicateElemAcc cat [] [1 1] [2 3]
--> duplicateElemAcc [1 1] [2 3]
--> duplicateElemAcc cat [1 1] [2 2] [3]
--> duplicateElemAcc [1 1 2 2] [3]
--> duplicateElemAcc cat [1 1 2 2] [3 3] []
--> duplicateElemAcc [1 1 2 2 3 3] []
--> [1 1 2 2 3 3]
```
<p>
Naturally, there's a much simpler way to define this using functions defined in the prelude.
(So much so, that you probably wouldn't define duplicateElem in a program, and instead 
write it explicitly).
</p>
```
duplicateElem => each [copy]
# Where each and copy are defined in the prelude as:
eachAcc a f [] => a;
eachAcc a f [x .. xs] => eachAcc cat a do [unquote f x] f xs;

each => eachAcc [];

copy x => x x;
```
<h2> The REPL </h2>
<p>
There are a few special commands on the REPL which all begin with #.
The most important one is '#load <filename>', because you can only declare rules in
files. If you load the same file more than once, it will reload the rule definitions.
Declaration files must begin with "version 0" on a line. This is so that future
interpreters can be backwards compatable without having to bend the language
to its beginnings.
</p>
<p>
#steps and #slow are useful for debugging. They will toggle whether steps are shown
and whether the steps are displayed slowly (a 100 ms delay between each step). This
is incredibly useful for debugging, as you can see how your expression evaluates.
</p>
<p>
I will note once again that this is a toy language: it doesn't have proper modularity,
it doesn't have typing to protect from a fair number of crashes mainly around arithmetic,
and it doesn't really have any useful libraries. It does have an extension system that
I'm not going to document right now as it's in development.
</p>
<p>
See the wiki for documentation of all the prelude rules.
</p>
