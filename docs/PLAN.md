# Plan prac: Roslyn Analyzer — metody `void` mutujące parametr

## Cel

Stworzyć analizator Roslyn dla C#, który zgłasza wyłącznie takie metody `void`,
które modyfikują przekazany parametr oraz mogą zostać automatycznie naprawione.

## Zakres pierwszej wersji

Diagnostyka obejmie tylko metodę, która spełnia wszystkie warunki:

- zwraca `void`;
- ma dokładnie jeden parametr typu referencyjnego;
- bezpośrednio przypisuje wartość do pola lub właściwości tego parametru;
- nie używa parametrów `ref` ani `out`;
- nie zawiera wcześniejszego `return;`;
- nie zawiera innych sytuacji wymagających złożonej zmiany zachowania metody.

Jeżeli automatyczna poprawka nie będzie mogła bezpiecznie obsłużyć przypadku,
analizator nie pokaże diagnostyki.

## Automatyczna poprawka

Code fix:

1. zmieni typ zwracany metody z `void` na typ parametru;
2. doda na końcu metody instrukcję `return parametr;`.

Przykład:

```csharp
void UstawNazwe(User user)
{
    user.Name = "Ala";
}
```

po poprawce:

```csharp
User UstawNazwe(User user)
{
    user.Name = "Ala";
    return user;
}
```

## Etapy realizacji

1. Utworzyć rozwiązanie z projektem Roslyn Analyzer i projektem testowym.
2. Zdefiniować identyfikator, kategorię i opis diagnostyki.
3. Zaimplementować analizę deklaracji metody i jej ciała.
4. Dodać code fix dla obsługiwanych metod.
5. Napisać testy pozytywne, negatywne i testy poprawki.
6. Uruchomić testy oraz ręcznie sprawdzić działanie w przykładowym kodzie.

## Poza zakresem pierwszej wersji

- aktualizacja miejsc wywołania metody;
- wiele mutowanych parametrów;
- mutacje wykrywane wyłącznie pośrednio przez wywołania innych metod;
- struktury i scenariusze z `ref` lub `out`.
