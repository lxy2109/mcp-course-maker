import test from 'ava'

var path = require('path')
  , after = require('after')
  , methods = require('methods')
  , assert = require('assert');

var Route = require('../lib/router/route')

test.cb('should work without handlers', t => {
  var req = { method: 'GET', url: '/' }
  var route = new Route('/foo')
  route.dispatch(req, {}, t.end)
})

// describe('.all ', function(){
test.cb('.all should add handler', t => {
  var req = { method: 'GET', url: '/' };
  var route = new Route('/foo');

  route.all(function(req, res, next) {
    req.called = true;
    next();
  });

  route.dispatch(req, {}, function (err) {
    t.ifError(err)
    // should(req.called).be.ok;
    t.true(req.called)
    t.end();
  });
})

test.cb('.all should handle VERBS', t => {
  var count = 0;
  var route = new Route('/foo');
  var cb = after(methods.length, function (err) {
    t.ifError(err)
    // count.should.equal(methods.length);
    t.is(count, methods.length)
    t.end();
  });

  route.all(function(req, res, next) {
    count++;
    next();
  });

  methods.forEach(function testMethod(method) {
    var req = { method: method, url: '/' };
    route.dispatch(req, {}, cb);
  });
})

test.cb('.all should stack', t => {
  var req = { count: 0, method: 'GET', url: '/' };
  var route = new Route('/foo');

  route.all(function(req, res, next) {
    req.count++;
    next();
  });

  route.all(function(req, res, next) {
    req.count++;
    next();
  });

  route.dispatch(req, {}, function (err) {
    t.ifError(err)
    // req.count.should.equal(2);
    t.is(req.count, 2)
    t.end();
  });
})

// .VERB
test.cb('.VERB should support .get', t => {
  var req = { method: 'GET', url: '/' };
  var route = new Route('');

  route.get(function(req, res, next) {
    req.called = true;
    next();
  })

  route.dispatch(req, {}, function (err) {
    t.ifError(err)
    // should(req.called).be.ok;
    t.true(req.called)
    t.end();
  });
})

test.cb('.VERB should limit to just .VERB', t => {
  var req = { method: 'POST', url: '/' };
  var route = new Route('');

  route.get(function(req, res, next) {
    throw new Error('not me!');
  })

  route.post(function(req, res, next) {
    req.called = true;
    next();
  })

  route.dispatch(req, {}, function (err) {
    t.ifError(err)
    t.true(req.called)
    t.end();
  });
})

test.cb('.VERB should allow fallthrough', t => {
  var req = { order: '', method: 'GET', url: '/' };
  var route = new Route('');

  route.get(function(req, res, next) {
    req.order += 'a';
    next();
  })

  route.all(function(req, res, next) {
    req.order += 'b';
    next();
  });

  route.get(function(req, res, next) {
    req.order += 'c';
    next();
  })

  route.dispatch(req, {}, function (err) {
    t.ifError(err)
    // req.order.should.equal('abc');
    t.is(req.order, 'abc')
    t.end();
  });
})


// errors 
test.cb('errors should handle errors via arity 4 functions', t => {
  var req = { order: '', method: 'GET', url: '/' };
  var route = new Route('');

  route.all(function(req, res, next){
    next(new Error('foobar'));
  });

  route.all(function(req, res, next){
    req.order += '0';
    next();
  });

  route.all(function(err, req, res, next){
    req.order += 'a';
    next(err);
  });

  route.dispatch(req, {}, function (err) {
    // should(err).be.ok;
    t.truthy(err)
    // should(err.message).equal('foobar');
    t.is(err.message, 'foobar')
    // req.order.should.equal('a');
    t.is(req.order, 'a')
    t.end();
  });
})

test.cb('errors should handle throw', t => {
  var req = { order: '', method: 'GET', url: '/' };
  var route = new Route('');

  route.all(function(req, res, next){
    throw new Error('foobar');
  });

  route.all(function(req, res, next){
    req.order += '0';
    next();
  });

  route.all(function(err, req, res, next){
    req.order += 'a';
    next(err);
  });

  route.dispatch(req, {}, function (err) {
    // should(err).be.ok;
    // t.ifError(err)
    // console.log(err)
    
    // should(err.message).equal('foobar');
    t.is(err.message, 'foobar')
    // req.order.should.equal('a');
    t.is(req.order, 'a')
    t.end();
  });
});

test.cb('errors should handle throwing inside error handlers', t => {
  var req = { method: 'GET', url: '/' };
  var route = new Route('');

  route.get(function(req, res, next){
    throw new Error('boom!');
  });

  route.get(function(err, req, res, next){
    throw new Error('oops');
  });

  route.get(function(err, req, res, next){
    req.message = err.message;
    next();
  });

  route.dispatch(req, {}, function (err) {
    t.ifError(err)
    // should(req.message).equal('oops');
    t.is(req.message, 'oops');
    t.end();
  });
});

test.cb('errors should handle throw in .all', t => {
  var req = { method: 'GET', url: '/' };
  var route = new Route('');

  route.all(function(req, res, next){
    throw new Error('boom!');
  });

  route.dispatch(req, {}, function(err){
    // should(err).be.ok;
    // t.ifError(err)
    t.truthy(err)
    // err.message.should.equal('boom!');
    t.is(err.message,'boom!');
    t.end();
  });
});

test.cb('errors should handle single error handler', t => {
  var req = { method: 'GET', url: '/' };
  var route = new Route('');

  route.all(function(err, req, res, next){
    // this should not execute
    // true.should.be.false;
    t.fail()
  });

  route.dispatch(req, {}, t.end);
});
